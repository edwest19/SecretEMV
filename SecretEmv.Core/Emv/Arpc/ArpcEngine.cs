// Copyright (c) 2026 edwest19
// This file was generated using Microsoft Copilot.
// EMV Engine: Generates ARPC (Authorization Response Cryptogram) from ARQC using issuer 3DES keys.

using SecretEmv.Core.Crypto;
using SecretEmv.Core.Primitives;
using SecretEmv.Core.Models;
using System;

namespace SecretEmv.Core.Emv.Arpc
{
    /// <summary>
    /// Implements EMV ARPC generation (3DES variant).
    /// Supports:
    /// - Method 1: ARPC = 3DES(SK_ARPC, ARQC XOR ARC)
    /// - Method 2: ARPC = 3DES(SK_ARPC, ARQC) XOR ARC
    /// 
    /// SK_ARPC is typically derived from issuer keys; this class focuses on
    /// the cryptogram transformation itself.
    /// </summary>
    public class ArpcEngine
    {
        private readonly TripleDesEngine _tdes = new TripleDesEngine();

        /// <summary>
        /// Generates ARPC using EMV Method 1:
        /// ARPC = 3DES(SK_ARPC, ARQC XOR ARC)
        /// </summary>
        /// <param name="arqc">ARQC (8 bytes).</param>
        /// <param name="arc">Authorization Response Code (2 bytes, hex string).</param>
        /// <param name="sessionKey">SK_ARPC (3DES key bytes).</param>
        /// <returns>ARPC (8-byte cryptogram).</returns>
        public byte[] GenerateMethod1(byte[] arqc, string arc, byte[] sessionKey)
        {
            if (arqc == null)
                throw new ArgumentNullException(nameof(arqc));

            if (sessionKey == null)
                throw new ArgumentNullException(nameof(sessionKey));

            if (arqc.Length != 8)
                throw new ArgumentException("ARQC must be exactly 8 bytes.");

            if (string.IsNullOrWhiteSpace(arc))
                throw new ArgumentNullException(nameof(arc));

            // ARC is 2 bytes; pad to 8 bytes as per issuer profile (ARC || 0x00...00)
            byte[] arcBytes = HexUtils.FromHex(arc);
            if (arcBytes.Length != 2)
                throw new ArgumentException("ARC must be exactly 2 bytes.");

            byte[] arcBlock = ConcatUtils.Concat(arcBytes, new byte[6]); // 2 + 6 = 8

            // ARQC XOR ARC-block
            byte[] input = XorUtils.Xor(arqc, arcBlock);

            // ARPC = 3DES(SK_ARPC, input)
            return _tdes.EncryptBlock(sessionKey, input);
        }

        public ArpcResult GenerateArpc(string arqcHex, string arcHex, string sessionKeyHex)
        {
            // Convert inputs
            byte[] arqc = Convert.FromHexString(arqcHex);
            byte[] arc = Convert.FromHexString(arcHex);
            byte[] skac = Convert.FromHexString(sessionKeyHex);

            // EMV ARPC Method 1: ARPC = 3DES(SK_AC, ARQC XOR ARC)
            byte[] xored = new byte[arqc.Length];
            for (int i = 0; i < arqc.Length; i++)
                xored[i] = (byte)(arqc[i] ^ arc[i]);

            byte[] arpc = _tdes.EncryptBlock(skac, xored);


            return new ArpcResult
            {
                Arpc = Convert.ToHexString(arpc)
            };
        }


        /// <summary>
        /// Generates ARPC using EMV Method 2:
        /// ARPC = 3DES(SK_ARPC, ARQC) XOR ARC
        /// </summary>
        /// <param name="arqc">ARQC (8 bytes).</param>
        /// <param name="arc">Authorization Response Code (2 bytes, hex string).</param>
        /// <param name="sessionKey">SK_ARPC (3DES key bytes).</param>
        /// <returns>ARPC (8-byte cryptogram).</returns>
        public byte[] GenerateMethod2(byte[] arqc, string arc, byte[] sessionKey)
        {
            if (arqc == null)
                throw new ArgumentNullException(nameof(arqc));

            if (sessionKey == null)
                throw new ArgumentNullException(nameof(sessionKey));

            if (arqc.Length != 8)
                throw new ArgumentException("ARQC must be exactly 8 bytes.");

            if (string.IsNullOrWhiteSpace(arc))
                throw new ArgumentNullException(nameof(arc));

            // ARC is 2 bytes; pad to 8 bytes as per issuer profile (ARC || 0x00...00)
            byte[] arcBytes = HexUtils.FromHex(arc);
            if (arcBytes.Length != 2)
                throw new ArgumentException("ARC must be exactly 2 bytes.");

            byte[] arcBlock = ConcatUtils.Concat(arcBytes, new byte[6]); // 2 + 6 = 8

            // First: Y = 3DES(SK_ARPC, ARQC)
            byte[] y = _tdes.EncryptBlock(sessionKey, arqc);

            // ARPC = Y XOR ARC-block
            return XorUtils.Xor(y, arcBlock);
        }
    }
}
