// Copyright (c) 2026 edwest19
// All rights reserved.
// This file was generated using Microsoft Copilot.

using SecretEmv.Core.Emv.MasterKeyDerivation;

class Program
{
    static void Main(string[] args)
    {
        string imkHex = args[0];
        string pan = args[1];
        string psn = args[2];

        byte[] imk = Convert.FromHexString(imkHex);

        var deriver = new DesMasterKeyDeriver();
        byte[] mkac = deriver.DeriveOptionA(imk, pan, psn);

        Console.WriteLine(Convert.ToHexString(mkac));
    }
}
