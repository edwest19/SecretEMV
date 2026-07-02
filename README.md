# 📘 SecretEmv  
A complete EMV cryptographic engine for Windows (.NET 10 / WinUI 3), implementing:

- ICC Master Key derivation (DES & AES)
- Session Key derivation (DES & AES)
- ARQC generation
- ARPC generation
- EMV DOL parsing
- EMV AC pipeline
- Developer‑friendly tooling for EMV research, testing, and education

SecretEmv is designed as a modular, deterministic, transparent EMV cryptography toolkit.  
It is **not** a payment application — it is a **developer tool** for understanding EMV cryptographic flows.
The idea for this project is for Copilot to generate the entire solution, including all EMV flows, based on the EMV cryptographic summary provided in this README.md and the ppublic EMV specifications.

---

# 🚀 Features

### ✔ EMV Master Key Derivation  
- Option A (PAN16)  
- Option B (PAN16 + PSN nibble padded)  
- AES Option 3 (CMAC‑based diversification)

### ✔ Session Key Derivation  
- 3DES ATC diversification using F0/0F blocks  
- AES‑CMAC(IMK‑AES, ATC || 14×00)  

### ✔ ARQC Generation  
- MAC generation using Session key, UN, DOL parsing

### ✔ ARPC Generation  
- ARQC + ARC  
- MAC generation

### ✔ Modular Architecture  
- Core crypto engines  
- EMV primitives  
- WinUI front‑end  
- MasterKey tool  
- SessionKey tool  
- ARQC/ARPC tool

---

# 🧱 Project Structure

```
SecretEmv/
│
├── README.md                 ← master prompt (this file)
├── LICENSE
├── SecretEmv.slnx
├── .gitignore
│
├── SecretEmv.Core/           ← EMV crypto engines
│   ├── Crypto/
│   ├── Emv/
│   ├── Logging/
│   ├── Models/
│   ├── Primitives/
│
├── SecretEmv.GenAc/            ← WinUI 3 application
│
└── SecretEmv.MasterKey/      ← CLI tool for MK derivation
└── SecretEmv.SKD/            ← CLI tool for SKD derivation
└── SecretEmv.Arqc/      ← CLI tool for ARQC/ARPC derivation

```

---

# 🛠 Build Instructions

### Requirements
- Windows 10/11  
- Visual Studio 2022  
- .NET 10  
- Windows App SDK  
- WinUI 3  

### Build
```
dotnet build SecretEmv.sln
```

### Run WinUI App
```
dotnet run --project SecretEmv.App
```

### Run MasterKey Tool
```
dotnet run --project SecretEmv.MasterKey
```

---

# 📚 EMV Cryptographic Summary (Used by Copilot Agent)

This section provides a **safe, non‑copyrighted summary** of the EMV cryptographic rules required for SecretEmv.  
Copilot Agent should use these rules when completing or modifying EMV code.

---

## 🔐 ICC Master Key Derivation

### **DES Option A**
- Take rightmost 16 digits of PAN → 8 bytes  
- Encrypt with IMK‑AC using 3DES  
- Output = MK‑AC (8 bytes)

### **DES Option B**
- PAN16 → 8 bytes  
- PSN nibble → last hex digit  
- Pad nibble with `F` → `(PSN << 4) | 0x0F`  
- Step 1: `K1 = 3DES(IMK, PAN16)`  
- Step 2: XOR last byte of K1 with padded nibble  
- Step 3: `MK_AC = 3DES(IMK, K1_modified)`

### **AES Option 3**
- Diversification block = `PAN16 || 14×00`  
- `MK_AC = AES-CMAC(IMK-AES, diversification_block)`

---

## 🔑 Session Key Derivation (SK_AC)

### **DES SK_AC**
Two diversification blocks:

```
B1 = ATC || F0 || 00 || 00 || 00 || 00
B2 = ATC || 0F || 00 || 00 || 00 || 00
```

Encrypt both with MK‑AC:

```
Left  = 3DES(MK_AC, B1)
Right = 3DES(MK_AC, B2)
SK_AC = Left || Right   (16 bytes)
```

### **AES SK_AC (Option 3)**
Diversification block:

```
ATC || 14×00   (16 bytes)
```

Session key:

```
SK_AC = AES-CMAC(IMK-AES, diversification_block)
```

---

## 🧮 ARQC Generation

Inputs:

- SK_AC  
- UN (Unpredictable Number)  
- DOL (Data Object List)  
- Transaction data  

Process:

1. Build DOL data block  
2. Compute MAC using SK_AC  
3. Output ARQC (8 or 16 bytes depending on algorithm)

---

## 🔁 ARPC Generation

Inputs:

- ARQC  
- ARC (Authorization Response Code)  
- SK_AC  

Process:

1. Build ARPC input block  
2. Compute MAC using SK_AC  
3. Output ARPC

---

# 🧠 Copilot Development Prompt (Master Instructions)

Copilot Agent must follow **all** instructions in this section when analyzing, modifying, or extending the SecretEmv solution.

---

## 🔧 Architecture Rules

- Maintain strict modular separation:
  - `Crypto` = cryptographic primitives  
  - `Emv` = EMV logic  
  - `Primitives` = data structures  
  - `Utilities` = helpers  
- No business logic in crypto engines  
- No UI logic in core libraries  
- All EMV operations must be deterministic  
- No network calls  
- No external dependencies beyond .NET + Windows App SDK  
- No guessing or inference in EMV cryptographic flows  
- All algorithms must match the EMV summary above

---

## 🧪 Development Rules

- All TODOs must be completed  
- All placeholder code must be replaced with real implementations  
- All EMV flows must be fully wired end‑to‑end  
- All cryptographic operations must use:
  - TripleDesEngine  
  - AesCmacEngine  
- All DOL parsing must be strict and deterministic  
- All ARQC/ARPC generation must follow the EMV summary  
- All errors must be explicit and actionable  
- All code must be safe, deterministic, and reproducible

---

## 🧭 Copilot Agent Behavior

When Copilot Agent is invoked:

- Read this README.md fully  
- Treat the “Copilot Development Prompt” section as the **authoritative system prompt**  
- Scan the entire solution  
- Identify:
  - TODOs  
  - incomplete implementations  
  - placeholders  
  - missing EMV steps  
  - missing wiring  
- Generate a plan  
- Apply fixes according to the rules above  
- Do not modify cryptographic primitives unless required  
- Do not introduce external dependencies  
- Do not alter the EMV pipeline structure  
- Maintain deterministic behavior across all engines

---

# 📄 License

- MIT  

---

# 🎯 Final Notes

This README.md acts as:

- GitHub documentation  
- Developer onboarding  
- EMV crypto summary  
- Copilot Agent master prompt  
- Architecture guide  
- Project specification  

---

## Release v1.0.0 - EMV Cryptographic Toolkit

### Features
- ✅ EMV Master Key Derivation (3DES Option A/B, AES)
- ✅ Session Key Derivation (3DES, AES-CMAC)
- ✅ ARQC Generation (ISO 9797-1 Algorithm 3)
- ✅ ARPC Generation (Method 1 & CSU-based)
- ✅ WinUI 3 Desktop Application
- ✅ CLI Tools (MasterKey, SKD, ARQC)
- ✅ EMV 4.3 Book 2 Spec Compliant

### Verified Against EMV Spec Examples
- A.3.1: Master Key Derivation
- A.3.1.1: Long PAN (Option B with SHA-1)
- A.3.2: Session Key Derivation
- A.3.3: ARQC Generation ✓ `C20039270FE384D5`
- A.3.4: ARPC Generation ✓ `90EF477F`

### Known Issues
- EMV spec A.3.1.1 contains an error in intermediate calculation; our implementation is mathematically correct