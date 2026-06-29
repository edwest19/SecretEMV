# SecretEmv — Split Service Architecture

> Replaces the monolithic `EmvCryptoService` proposed in the original spec.  
> Follows EMV Book 2 v4.4. All services registered as singletons via DI.

---

## Design Principle

Each service owns exactly one EMV concern. Dependencies flow in one direction:
`ViewModel → EmvOrchestrationService → {MasterKeyService | SessionKeyService | CryptogramService}`

Low-level primitives (`TripleDesService`, `AesCmacService`, `HexService`) are
injected into the domain services — never called directly from a ViewModel.

---

## Revised Services/ Folder

```
Services/
├── Primitives/
│   ├── HexService.cs              # hex encode/decode, byte utilities
│   ├── TripleDesService.cs        # 3DES ECB/CBC encrypt/decrypt
│   └── AesCmacService.cs         # AES-CMAC per NIST SP 800-38B
│
├── MasterKeyService.cs            # ICC Master Key Derivation (Book 2 Annex A1.4)
├── SessionKeyService.cs           # Session Key Derivation SK_AC (Book 2 Annex A1.3)
├── CryptogramService.cs           # ARQC generation + ARPC Methods 1 & 2 (Book 2 §8)
├── TransactionDataService.cs      # EMV tag list builder (stub → real in v2)
├── EmvOrchestrationService.cs     # Composes the above; called by ViewModel
│
├── SettingsService.cs             # JSON persistence (unchanged)
└── LogService.cs                  # In-memory log (unchanged)
```

---

## 1. Primitive Layer

### HexService
```csharp
public interface IHexService
{
    byte[] FromHex(string hex);
    string ToHex(byte[] bytes);
    string ToHexSpaced(byte[] bytes);        // "A1 B2 C3 …" for log display
    byte[] XorBytes(byte[] a, byte[] b);
}
```
No EMV knowledge. Pure byte manipulation.

---

### TripleDesService
```csharp
public interface ITripleDesService
{
    // All keys are 16-byte (2-key 3DES) or 24-byte (3-key 3DES)
    byte[] EncryptEcb(byte[] key, byte[] data);
    byte[] DecryptEcb(byte[] key, byte[] data);
    byte[] EncryptCbc(byte[] key, byte[] iv, byte[] data);
    byte[] Mac3Des(byte[] key, byte[] data);   // Retail MAC per ISO 9797-1 Alg 3
}
```

---

### AesCmacService
```csharp
public interface IAesCmacService
{
    // key: 16, 24, or 32 bytes (AES-128/192/256)
    byte[] ComputeCmac(byte[] key, byte[] data);
    byte[] EncryptEcb(byte[] key, byte[] data);   // used by AES Option 3 derivation
}
```

---

## 2. Domain Layer

### MasterKeyService
**Owns:** ICC Master Key Derivation — all three options from Book 2 Annex A1.4.

```csharp
public interface IMasterKeyService
{
    // Option A — PAN decimalisation (Figure 14 decimalisation table)
    CardMasterKeyResult DeriveOptionA(CardMasterKeyInput input);

    // Option B — PAN block (no decimalisation)
    CardMasterKeyResult DeriveOptionB(CardMasterKeyInput input);

    // Option 3 — AES-CMAC (Spec Bulletin 162 erratum applied)
    CardMasterKeyResult DeriveOptionAes(CardMasterKeyInput input);
}
```

**Internal flow for Option A:**
1. Concatenate PAN (rightmost 12 digits, excl. check digit) + PSN → 16-byte block
2. Apply decimalisation per Figure 14
3. Encrypt left half with IMK-AC using 3DES ECB → left result
4. XOR block with `0xFFFFFFFFFFFFFFFF`, encrypt → right result
5. Concatenate → Card Master Key (CMK)
6. Log all intermediate values via `ILogService`

**Internal flow for Option 3 (AES):**
1. Build diversification data per Spec Bulletin 162 (not Annex A1.4 alone)
2. Compute AES-CMAC of diversification data under IMK-AC
3. For AES-256: compute second CMAC with modified input, concatenate
4. Log all intermediate values

**Dependencies:** `ITripleDesService`, `IAesCmacService`, `IHexService`, `ILogService`

---

### SessionKeyService
**Owns:** Session Key Derivation (SK_AC) per Book 2 Annex A1.3.

```csharp
public interface ISessionKeyService
{
    // Derives SK_AC from CMK, ATC, and (for 3DES) UN
    SessionKeyResult DeriveSessionKey(SessionKeyInput input);
}
```

**Internal flow (3DES):**
1. Build left diversification block: ATC (2 bytes) + `0x0000` + `0x000000000000` (padding per spec)
2. Build right diversification block: ATC (2 bytes) + `0xFFFF` + `0x000000000000`
3. Encrypt each block with CMK using 3DES ECB
4. Concatenate left + right → SK_AC (16 bytes)

**Internal flow (AES):**
1. Build 16-byte diversification data: `0x0101` + ATC + `0x00...`
2. Compute AES-CMAC under CMK

**Note:** UN is not used in standard SK_AC derivation — it enters at ARQC generation.
`SessionKeyInput` carries it for completeness but `SessionKeyService` ignores it.

**Dependencies:** `ITripleDesService`, `IAesCmacService`, `IHexService`, `ILogService`

---

### CryptogramService
**Owns:** ARQC generation and ARPC response — Book 2 §8.1 and §8.2.

```csharp
public interface ICryptogramService
{
    // §8.1 — Generate ARQC from SK_AC over transaction data
    ArqcResult GenerateArqc(ArqcInput input);

    // §8.2 Method 1 — XOR ARQC with ARC, encrypt with SK_AC
    ArpcResult GenerateArpcMethod1(ArpcInput input);

    // §8.2 Method 2 — MAC over CSU + proprietary data with SK_AC
    ArpcResult GenerateArpcMethod2(ArpcInput input);
}
```

**ARQC internal flow (3DES):**
1. Receive pre-built transaction data byte array from `ITransactionDataService`
2. Pad to 8-byte boundary per ISO 9797-1 (Method 2 padding)
3. Compute Retail MAC (ISO 9797-1 Algorithm 3) under SK_AC
4. First 8 bytes = ARQC

**ARQC internal flow (AES):**
1. Receive transaction data
2. Compute AES-CMAC under SK_AC
3. First 8 bytes = ARQC

**ARPC Method 1:**
1. XOR ARQC (8 bytes) with ARC (2 bytes, zero-padded to 8)
2. Encrypt result with SK_AC using 3DES ECB
3. Result = ARPC

**ARPC Method 2:**
1. Build 8-byte data block: CSU (4 bytes) + proprietary auth data (4 bytes, zero if absent)
2. Compute MAC over block using SK_AC
3. Result = ARPC

**Dependencies:** `ITransactionDataService`, `ITripleDesService`, `IAesCmacService`,
`IHexService`, `ILogService`

---

### TransactionDataService (Stub → Real)
**Owns:** EMV transaction data block construction.

```csharp
public interface ITransactionDataService
{
    // v1: caller supplies raw hex; service validates length and pads
    byte[] BuildFromRaw(string hexData);

    // v2 (future): build from named EMV fields
    byte[] BuildFromFields(TransactionFields fields);
}

public record TransactionFields
{
    public string AmountAuthorized { get; init; }   // Tag 9F02 — 6 bytes BCD
    public string AmountOther      { get; init; }   // Tag 9F03 — 6 bytes BCD
    public string TerminalCountry  { get; init; }   // Tag 9F1A — 2 bytes
    public string Tvr              { get; init; }   // Tag 95   — 5 bytes
    public string CurrencyCode     { get; init; }   // Tag 5F2A — 2 bytes
    public string TransactionDate  { get; init; }   // Tag 9A   — 3 bytes BCD
    public string TransactionType  { get; init; }   // Tag 9C   — 1 byte
    public string Un               { get; init; }   // Tag 9F37 — 4 bytes
    public string Atc              { get; init; }   // Tag 9F36 — 2 bytes
    public string Iad              { get; init; }   // Tag 9F10 — variable
}
```

In v1, `BuildFromFields` can throw `NotImplementedException` — Copilot
will scaffold it but you control when it goes live.

---

### EmvOrchestrationService
**Owns:** The only service the ViewModel ever calls.

```csharp
public interface IEmvOrchestrationService
{
    CardMasterKeyResult  GenerateCardMasterKey(CardMasterKeyInput input);
    SessionKeyResult     GenerateSessionKey(SessionKeyInput input);
    ArqcResult           GenerateArqc(ArqcInput input);
    ArpcResult           GenerateArpc(ArpcInput input);
}
```

**Implementation:**
```csharp
public class EmvOrchestrationService : IEmvOrchestrationService
{
    private readonly IMasterKeyService    _masterKey;
    private readonly ISessionKeyService   _sessionKey;
    private readonly ICryptogramService   _cryptogram;

    public EmvOrchestrationService(
        IMasterKeyService masterKey,
        ISessionKeyService sessionKey,
        ICryptogramService cryptogram)
    {
        _masterKey  = masterKey;
        _sessionKey = sessionKey;
        _cryptogram = cryptogram;
    }

    public CardMasterKeyResult GenerateCardMasterKey(CardMasterKeyInput input)
        => input.Cipher switch
        {
            EmvCipher.TripleDes when input.Method == DerivationMethod.OptionA
                => _masterKey.DeriveOptionA(input),
            EmvCipher.TripleDes when input.Method == DerivationMethod.OptionB
                => _masterKey.DeriveOptionB(input),
            EmvCipher.Aes
                => _masterKey.DeriveOptionAes(input),
            _ => throw new ArgumentOutOfRangeException()
        };

    public SessionKeyResult GenerateSessionKey(SessionKeyInput input)
        => _sessionKey.DeriveSessionKey(input);

    public ArqcResult GenerateArqc(ArqcInput input)
        => _cryptogram.GenerateArqc(input);

    public ArpcResult GenerateArpc(ArpcInput input)
        => input.Method == ArpcMethod.Method1
            ? _cryptogram.GenerateArpcMethod1(input)
            : _cryptogram.GenerateArpcMethod2(input);
}
```

The ViewModel calls `IEmvOrchestrationService` only. It never imports a cipher
primitive or knows whether 3DES or AES is executing.

---

## 3. DI Registration

```csharp
// DI/ServiceRegistration.cs
public static class ServiceRegistration
{
    public static IServiceCollection AddSecretEmvServices(
        this IServiceCollection services)
    {
        // Primitives
        services.AddSingleton<IHexService,      HexService>();
        services.AddSingleton<ITripleDesService, TripleDesService>();
        services.AddSingleton<IAesCmacService,   AesCmacService>();

        // Infrastructure
        services.AddSingleton<ISettingsService,  SettingsService>();
        services.AddSingleton<ILogService,       LogService>();

        // Domain
        services.AddSingleton<ITransactionDataService, TransactionDataService>();
        services.AddSingleton<IMasterKeyService,        MasterKeyService>();
        services.AddSingleton<ISessionKeyService,       SessionKeyService>();
        services.AddSingleton<ICryptogramService,       CryptogramService>();

        // Orchestration
        services.AddSingleton<IEmvOrchestrationService, EmvOrchestrationService>();

        // ViewModel
        services.AddSingleton<AcGenViewModel>();

        return services;
    }
}
```

---

## 4. Model Changes

Add two enums that the orchestration switch needs:

```csharp
// Models/EmvCipher.cs
public enum EmvCipher { TripleDes, Aes }

// Models/DerivationMethod.cs
public enum DerivationMethod { OptionA, OptionB }

// Models/ArpcMethod.cs
public enum ArpcMethod { Method1, Method2 }
```

Update `CardMasterKeyInput` to carry `EmvCipher` and `DerivationMethod`
instead of a raw string selector. This makes the orchestration switch
exhaustive and compiler-verified.

---

## 5. ViewModel Impact

`AcGenViewModel` shrinks considerably. It holds:
- Bound properties (IMK, PAN, PSN, ATC, UN, cipher selection, key length)
- Four `ICommand` implementations that call `IEmvOrchestrationService`
- Result properties (`CardMasterKey`, `SessionKey`, `Arqc`, `Arpc`)
- `LogLines` bound to `ILogService.Get()`

No cipher logic, no byte arrays, no EMV knowledge.

---

## 6. Copilot Prompt Strategy

Generate each file in this order to avoid forward-reference errors:

1. Enums (`EmvCipher`, `DerivationMethod`, `ArpcMethod`)
2. All Models
3. `IHexService` + `HexService`
4. `ITripleDesService` + `TripleDesService`
5. `IAesCmacService` + `AesCmacService`
6. `ILogService` + `LogService`
7. `ISettingsService` + `SettingsService`
8. `ITransactionDataService` + `TransactionDataService` (stub)
9. `IMasterKeyService` + `MasterKeyService`
10. `ISessionKeyService` + `SessionKeyService`
11. `ICryptogramService` + `CryptogramService`
12. `IEmvOrchestrationService` + `EmvOrchestrationService`
13. `ServiceRegistration`
14. `AcGenViewModel`
15. `AcGenPage.xaml` + `AcGenPage.xaml.cs`

Each Copilot prompt should reference only the interface and the spec section
number — not the implementation of sibling services. This keeps context
windows tight and output consistent.

---

## 7. Future Module Fit

When `SecretEmv.Dukpt` or `SecretEmv.TagBrowser` are added:

- They get their own `Services/` folder following the same primitive → domain → orchestration pattern
- They share `HexService` via a shared library project (`SecretEmv.Shared`)
- `LogService` and `SettingsService` move to `SecretEmv.Shared` at that point
- `EmvOrchestrationService` stays scoped to `SecretEmv.AcGen` — each module has its own orchestrator

No refactoring required in `AcGen` when new modules are added.
