// Copyright (c) 2026 edwest19
// This file was generated using Microsoft Copilot.
// Crypto Engine: Implements EMV Retail MAC (ISO 9797-1 Algorithm 3) using Triple DES CBC.

using System;
using SecretEmv.Core.Primitives;

namespace SecretEmv.Core.Crypto
{
    /// <summary>
    /// Provides EMV Retail MAC generation using ISO 9797-1 Algorithm 3 (3DES CBC).
    /// Used in EMV ARQC, ARPC, and other MAC-based cryptographic flows.
    /// </summary>
    public class RetailMacEngine
    {
        private readonly TripleDesEngine _tdes = new TripleDesEngine();

        /// <summary>
        /// Computes EMV Retail MAC using 3DES CBC with ISO9797-1 M2 padding.
        /// </summary>
        public byte[] ComputeMac(byte[] key, byte[] iv, byte[] data)
        {
            if (key == null || iv == null || data == null)
                throw new ArgumentNullException();

            if (iv.Length != 8)
                throw new ArgumentException("IV must be 8 bytes.");

            // Step 1: Apply ISO9797-1 Padding Method 2
            var padded = PaddingUtils.Iso9797M2(data, 8);

            // Step 2: Split into 8-byte blocks
            var blocks = BlockUtils.Split(padded, 8);

            // Step 3: CBC-MAC using 3DES
            byte[] current = iv;

            foreach (var block in blocks)
            {
                var xored = XorUtils.Xor(current, block);
                current = _tdes.EncryptBlock(key, xored);
            }

            // Step 4: Final MAC = last CBC block
            return current;
        }
    }
}
