# **SecretEmv.Emvco-Spec-Summary.md**  
### *EMVCo Cryptographic Specification Summary for Copilot Agent*

This document provides a safe, non‑copyrighted summary of the EMV cryptographic rules required for the SecretEmv project.  
Copilot Agent must use these rules when analyzing, modifying, or extending EMV cryptographic code.

---

# 📘 **1. Overview**

SecretEmv implements the EMV cryptographic pipeline:

- ICC Master Key derivation (DES & AES)
- AC Session Key derivation (DES & AES)
- ARQC generation
- ARPC generation
- DOL parsing
- EMV AC pipeline assembly

This summary defines the algorithmic behavior required for each step.

---

# 🔐 **2. ICC Master Key Derivation (MK‑AC)**

## **2.1 DES MK‑AC Option A (PAN16)**

Inputs:
- PAN (Primary Account Number)

Process:
1. Take the **rightmost 16 digits** of the PAN.
2. Convert to 8 bytes (BCD).
3. Encrypt with IMK‑AC using 3DES.
4. Output = **MK‑AC (8 bytes)**.

---

## **2.2 DES MK‑AC Option B (PAN16 + PSN nibble)**

Inputs:
- PAN16 (rightmost 16 digits of PAN)
- PSN (Primary Sequence Number, 1 nibble)

Process:
1. Convert PAN16 → 8 bytes.
2. Convert PSN nibble → 1 byte padded with `0xF` in the high nibble.
3. Step 1: `K1 = 3DES(IMK-AC, PAN16)`
4. Step 2: Modify last byte of K1 by XOR with padded PSN nibble.
5. Step 3: `MK_AC = 3DES(IMK-AC, K1_modified)`

Output:
- **MK‑AC (8 bytes)**

---

## **2.3 AES MK‑AC (AES Option 3)**

Inputs:
- PAN16 (rightmost 16 digits of PAN)
- IMK‑AES (16 bytes)

Process:
1. Build diversification block:  
   `PAN16 || 14×00` → 16 bytes total.
2. Compute:  
   `MK_AC = AES-CMAC(IMK-AES, diversification_block)`

Output:
- **MK‑AC (16 bytes)**

---

# 🔑 **3. AC Session Key Derivation (SK‑AC)**

## **3.1 DES SK‑AC (ATC Diversification)**

Inputs:
- MK‑AC (8 bytes)
- ATC (Application Transaction Counter, 2 bytes)

Process:
Build two diversification blocks:

```
B1 = ATC || F0 || 00 || 00 || 00 || 00
B2 = ATC || 0F || 00 || 00 || 00 || 00
```

Encrypt both with MK‑AC:

```
Left  = 3DES(MK_AC, B1)
Right = 3DES(MK_AC, B2)
SK_AC = Left || Right
```

Output:
- **SK‑AC (16 bytes)**

---

## **3.2 AES SK‑AC (AES Option 3)**

Inputs:
- MK‑AES (16 bytes)
- ATC (2 bytes)

Process:
1. Build diversification block:  
   `ATC || 14×00` → 16 bytes.
2. Compute:  
   `SK_AC = AES-CMAC(MK-AES, diversification_block)`

Output:
- **SK‑AC (16 bytes)**

---

# 🧮 **4. ARQC Generation**

Inputs:
- SK‑AC  
- UN (Unpredictable Number)  
- DOL (Data Object List)  
- Transaction data  

Process:
1. Parse DOL to build the transaction data block.
2. Concatenate UN + DOL data.
3. Compute MAC using SK‑AC:
   - DES: 3DES MAC  
   - AES: AES‑CMAC  
4. Output ARQC.

Output:
- **ARQC (8 or 16 bytes depending on algorithm)**

---

# 🔁 **5. ARPC Generation**

Inputs:
- ARQC  
- ARC (Authorization Response Code)  
- SK‑AC  

Process:
1. Build ARPC input block from ARQC and ARC.
2. Compute MAC using SK‑AC.
3. Output ARPC.

Output:
- **ARPC (8 or 16 bytes)**

---

# 📦 **6. DOL Parsing Rules**

- DOL is a list of tags and lengths.
- Each tag specifies a required data element.
- The engine must:
  - read each tag  
  - read its length  
  - fetch the corresponding transaction data  
  - concatenate in order  
- Missing data must produce a deterministic error.

---

# 🧠 **7. Copilot Agent Instructions**

Copilot Agent must:

- Use this file as the EMV cryptographic specification.
- Follow all algorithmic rules exactly.
- Complete all TODOs in EMV engines.
- Ensure deterministic, reproducible cryptographic behavior.
- Maintain strict separation between:
  - crypto primitives  
  - EMV logic  
  - UI  
  - utilities  
- Avoid external dependencies.
- Avoid network calls.
- Never modify cryptographic algorithms unless required by this spec.

---

# 📄 **8. Notes**

This summary is safe for inclusion in the repository and for use by Copilot Agent.  
It contains **no copyrighted EMVCo text** and is suitable for automated ingestion.

---

