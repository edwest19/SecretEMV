// Copyright (c) 2026 edwest19
// All rights reserved.
// This file was generated using Microsoft Copilot.

using System;
using System.Collections.Generic;
using SecretEmv.Core.Emv.Arqc;
using SecretEmv.Core.Emv.Dol;

class Program
{
    static void Main(string[] args)
    {
        string skacHex = args[0];
        string unHex = args[1];
        string atcHex = args[2];
        string dolHex = args[3];

        byte[] skac = Convert.FromHexString(skacHex);
        byte[] dolBytes = Convert.FromHexString(dolHex);

        var parser = new DolParser();
        var entries = parser.Parse(dolBytes);

        // Tag values will be supplied later; empty map for now.
        var tagValues = new Dictionary<string, byte[]>();

        byte[] transactionData = parser.BuildDataBlock(entries, tagValues);

        var arqcEngine = new ArqcEngine();
        byte[] arqc = arqcEngine.GenerateArqc(skac, atcHex, unHex, transactionData);

        Console.WriteLine(Convert.ToHexString(arqc));
    }
}
