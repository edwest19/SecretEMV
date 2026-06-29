// Copyright (c) 2026 edwest19
// This file was generated using Microsoft Copilot.
// EMV Engine: Provides DES/3DES ICC Master Key derivation (Option A and Option B).

using System;
using SecretEmv.Core.Primitives;
using SecretEmv.Core.Crypto;

namespace SecretEmv.Core.Emv.MasterKeyDerivation
{
    /// <summary>
    /// Implements EMV DES/3DES ICC Master Key derivation.
    /// Supports:
    /// - Option A (PAN decimalisation)
    /// - Option B (PAN block method)
    /// 
    /// This class produces the ICC Master Key (IMK) used for session key derivation.
    /// </summary>
    public class DesMasterKeyDeriver
    {
        private readonly TripleDesEngine _tdes = new TripleDesEngine();

        /// <summary>
        /// Derives the ICC Master Key using EMV Option A (PAN decimalisation).
        /// </summary>
        /// <param name="issuerMasterKey">The issuer master key (IMK).</param>
        /// <param name="pan">Primary Account Number (PAN).</param>
        /// <param name="sequenceNumber">Card sequence number (CSN).</param>
        /// <returns>Derived ICC Master Key.</returns>
        public byte[] DeriveOptionA(byte[] imk, string pan, string psn)
        {
            if (imk == null || imk.Length != 16)
                throw new ArgumentException("IMK-AC must be 16 bytes.");

            if (string.IsNullOrWhiteSpace(pan))
                throw new ArgumentNullException(nameof(pan));

            // 1. Take rightmost 16 digits of PAN
            string pan16 = pan.Length > 16 ? pan.Substring(pan.Length - 16) : pan;

            if (pan16.Length != 16)
                throw new ArgumentException("PAN must contain at least 16 digits.");

            // 2. Diversification block for Option A is ONLY PAN16 (8 bytes)
            byte[] diversificationData = Convert.FromHexString(pan16);

            if (diversificationData.Length != 8)
                throw new Exception("Diversification data must be exactly 8 bytes.");

            // 3. MK-AC = 3DES(IMK-AC, PAN16)
            var tdes = new TripleDesEngine();
            byte[] mkac = tdes.EncryptBlock(imk, diversificationData);

            return mkac;
        }

        /// <summary>
        /// Derives the ICC Master Key using EMV Option B (PAN block method).
        /// </summary>
        /// <param name="issuerMasterKey">The issuer master key (IMK).</param>
        /// <param name="pan">Primary Account Number (PAN).</param>
        /// <param name="sequenceNumber">Card sequence number (CSN).</param>
        /// <returns>Derived ICC Master Key.</returns>
        public byte[] DeriveOptionB(byte[] issuerMasterKey, string pan, string sequenceNumber)
        {
            if (issuerMasterKey == null || issuerMasterKey.Length != 16)
                throw new ArgumentException("IMK-AC must be 16 bytes.");

            if (string.IsNullOrWhiteSpace(pan))
                throw new ArgumentNullException(nameof(pan));

            if (string.IsNullOrWhiteSpace(sequenceNumber))
                throw new ArgumentNullException(nameof(sequenceNumber));

            // 1. Take rightmost 16 digits of PAN
            string pan16 = pan.Length > 16 ? pan.Substring(pan.Length - 16) : pan;

            if (pan16.Length != 16)
                throw new ArgumentException("PAN must contain at least 16 digits.");

            // Convert PAN16 to bytes (8 bytes)
            byte[] panBlock = Convert.FromHexString(pan16);

            // 2. Extract PSN nibble (last hex digit)
            char psnNibbleChar = sequenceNumber[sequenceNumber.Length - 1];

            // Validate nibble
            if (!Uri.IsHexDigit(psnNibbleChar))
                throw new ArgumentException("PSN must be hex.");

            // Convert nibble to value
            byte psnNibble = Convert.ToByte(psnNibbleChar.ToString(), 16);

            // 3. Pad nibble with F (EMV Option B rule)
            byte paddedNibble = (byte)((psnNibble << 4) | 0x0F);

            // 4. First 3DES encryption: K1 = 3DES(IMK, PAN16)
            var tdes = new TripleDesEngine();
            byte[] k1 = tdes.EncryptBlock(issuerMasterKey, panBlock);

            // 5. XOR last byte of K1 with padded nibble
            byte[] k1Modified = (byte[])k1.Clone();
            k1Modified[k1Modified.Length - 1] ^= paddedNibble;

            // 6. Second 3DES encryption: MK_AC = 3DES(IMK, K1Modified)
            byte[] mkac = tdes.EncryptBlock(issuerMasterKey, k1Modified);

            return mkac;
        }

        /// <summary>
        /// Builds the PAN block used in EMV Option B.
        /// </summary>
        private string BuildPanBlock(string pan)
        {
            // Rightmost 16 digits of PAN (excluding check digit)
            string trimmed = pan.Length > 16
                ? pan.Substring(pan.Length - 16)
                : pan;

            return trimmed;
        }
    }
}
