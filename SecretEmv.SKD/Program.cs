// Copyright (c) 2026 edwest19
// All rights reserved.
// This file was generated using Microsoft Copilot.

using SecretEmv.Core.Emv.SessionKeyDerivation;

class Program
{
    static void Main(string[] args)
    {
        string mkacHex = args[0];
        string diversificationHex = args[1];

        byte[] mkac = Convert.FromHexString(mkacHex);

        var deriver = new DesSessionKeyDeriver();
        byte[] skac = deriver.Derive(mkac, diversificationHex);

        Console.WriteLine(Convert.ToHexString(skac));
    }
}
