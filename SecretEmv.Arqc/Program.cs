// Copyright (c) 2026 edwest19
// All rights reserved.
// This file was generated using Microsoft Copilot.

using System;
using SecretEmv.Core.Crypto;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: SecretEmv.Arqc <session_key_hex> <transaction_data_hex>");
            Console.WriteLine();
            Console.WriteLine("Computes EMV ARQC (Retail MAC) over transaction data using the session key.");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  SecretEmv.Arqc 182025BA4FAB32F5A63A1BA5E6845D4E 000000010000...");
            return;
        }

        string sessionKeyHex = args[0];
        string transactionDataHex = args[1];

        try
        {
            // Parse inputs
            byte[] sessionKey = Convert.FromHexString(sessionKeyHex);
            byte[] transactionData = Convert.FromHexString(transactionDataHex);

            // Debug output
            Console.WriteLine($"Session Key: {sessionKeyHex}");
            Console.WriteLine($"Session Key Length: {sessionKey.Length} bytes");
            Console.WriteLine($"Transaction Data: {transactionDataHex}");
            Console.WriteLine($"Transaction Data Length: {transactionData.Length} bytes");
            Console.WriteLine();

            // Validate session key length
            if (sessionKey.Length != 16 && sessionKey.Length != 24)
            {
                Console.WriteLine($"ERROR: Session key must be 16 or 24 bytes (got {sessionKey.Length})");
                return;
            }

            // Compute ARQC using Retail MAC
            var macEngine = new RetailMacEngine();
            byte[] iv = new byte[8]; // EMV uses zero IV
            byte[] arqc = macEngine.ComputeMac(sessionKey, iv, transactionData);

            // Output ARQC
            Console.WriteLine($"ARQC: {Convert.ToHexString(arqc)}");
            Console.WriteLine($"Expected (EMV Spec A.3.3): C20039270FE384D5");  // Corrected!
        }
        catch (FormatException)
        {
            Console.WriteLine("ERROR: Invalid hex string format");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }
}