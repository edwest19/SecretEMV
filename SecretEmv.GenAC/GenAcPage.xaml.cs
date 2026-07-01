// Copyright (c) 2026 edwest19
// All rights reserved.
// This file was generated using Microsoft Copilot.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecretEmv.Core.Emv;
using SecretEmv.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SecretEmv.GenAC
{
    public sealed partial class AcGenPage : Page
    {
        // Add this constant at the class level (after line 16, inside the class)
        private static readonly HashSet<string> AllowedDolTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "9F02",  // Amount, Authorised (Numeric)
        "9F03",  // Amount, Other (Numeric)
        "9F1A",  // Terminal Country Code
        "95",    // Terminal Verification Results
        "5F2A",  // Transaction Currency Code
        "9A",    // Transaction Date
        "9C",    // Transaction Type
        "9F37",  // Unpredictable Number
        "82",    // Application Interchange Profile
        "9F36"   // Application Transaction Counter (ATC)
    };

    private readonly EmvCryptoPipelineService _pipeline = new EmvCryptoPipelineService();

        public AcGenPage()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Close the application
            Application.Current.Exit();
        }

        // -----------------------------
        // Section 1: Card Master Key
        // -----------------------------
        private void GenerateCardMasterKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string imkHex = TxtImkAc.Text.Trim();
                string pan = TxtPan.Text.Trim();
                string psn = TxtPsn.Text.Trim();

                byte[] issuerMasterKey = Convert.FromHexString(imkHex);

                byte[] mkac;

                if (Rb3Des.IsChecked == true)
                {
                    // 3DES Option A or B
                    if (Rb3DesOptionA.IsChecked == true)
                        mkac = _pipeline.DeriveDesIccMasterKeyOptionA(issuerMasterKey, pan, psn);
                    else
                        mkac = _pipeline.DeriveDesIccMasterKeyOptionB(issuerMasterKey, pan, psn);
                }
                else
                {
                    // AES Option 3
                    mkac = _pipeline.DeriveAesIccMasterKey(issuerMasterKey, pan, psn);
                }

                TxtCardMasterKey.Text = Convert.ToHexString(mkac);
                AppendLog("Card Master Key generated.");
            }
            catch (Exception ex)
            {
                AppendLog("ERROR (Card Master Key): " + ex.Message);
            }
        }

        // -----------------------------
        // Section 2: Session Key
        // -----------------------------
        private void GenerateSessionKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string mkacHex = TxtCardMasterKey.Text.Trim();
                string atcHex = TxtAtc.Text.Trim();

                byte[] mkac = Convert.FromHexString(mkacHex);

                byte[] skac;

                if (Rb3Des.IsChecked == true)
                    skac = _pipeline.DeriveDesSessionKey(mkac, atcHex);
                else
                    skac = _pipeline.DeriveAesSessionKey(mkac, atcHex);

                TxtSessionKey.Text = Convert.ToHexString(skac);
                AppendLog("Session Key generated.");
            }
            catch (Exception ex)
            {
                AppendLog("ERROR (Session Key): " + ex.Message);
            }
        }

        // -----------------------------
        // Section 3: ARQC
        // -----------------------------
        private void GenerateArqc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read DOL hex
                string dolHex = TxtDol.Text.Trim();

                // Convert DOL hex to bytes
                byte[] dolBytes = string.IsNullOrWhiteSpace(dolHex)
                    ? Array.Empty<byte>()
                    : Convert.FromHexString(dolHex);

                // Read tag values hex
                string tagValuesHex = TxtTagValues.Text.Trim();

                // Convert tag values hex to bytes
                byte[] tagValuesBytes = ParseTagValues(tagValuesHex);

                // Read Session Key SK_AC
                string sessionKeyHex = TxtSessionKey.Text.Trim();

                // Generate ARQC
                var result = _pipeline.GenerateArqc(
                    sessionKeyHex,
                    dolBytes,
                    tagValuesBytes
                );

                // Display ARQC
                TxtArqc.Text = result.Arqc;
                AppendLog("ARQC generated.");
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR (ARQC): {ex.Message}");
                AppendLog($"Stack: {ex.StackTrace}");
            }
        }


        // -----------------------------
        // Section 3: ARPC
        // -----------------------------
        private void GenerateArpc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read ARQC from UI
                string arqcHex = TxtArqc.Text.Trim();

                // Read ARC from UI (default 3030)
                string arcHex = TxtArc.Text.Trim();

                // Read Session Key SK_AC
                string sessionKeyHex = TxtSessionKey.Text.Trim();

                // Build ARPC input model
                var input = new ArpcInput
                {
                    Arqc = arqcHex,
                    Arc = arcHex,
                    SessionKeyAc = sessionKeyHex
                };

                // Generate ARPC
                var result = _pipeline.GenerateArpc(input);

                // Display ARPC
                TxtArpc.Text = result.Arpc;
                AppendLog("ARPC generated.");
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR (ARPC): {ex.Message}");
            }
        }

        private static byte[] ParseTagValues(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Array.Empty<byte>();

            return Convert.FromHexString(hex);
        }

    /// <summary>
    /// Handles DOL text changes - parses TLV format if detected and extracts only allowed tags
    /// </summary>
    private void TxtDol_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            string input = TxtDol.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
                return;

            byte[] data = Convert.FromHexString(input);

            // Try to parse as TLV and extract values for allowed tags only
            string extractedValues = ParseTlvWithFilter(data);

            if (!string.IsNullOrEmpty(extractedValues))
            {
                // Auto-populate tag values with only the allowed tags
                TxtTagValues.Text = extractedValues;
                AppendLog($"TLV data detected - extracted values for standard DOL tags.");
            }
        }
        catch
        {
            // Silently fail - user might still be typing
        }
    }

    /// <summary>
    /// Parses TLV data and extracts values only for tags in the allowed DOL list.
    /// Returns concatenated hex string of values in the order they appear in the TLV.
    /// </summary>
    private string ParseTlvWithFilter(byte[] data)
    {
        var valueList = new List<byte>();
        int offset = 0;

        while (offset < data.Length)
        {
            // Parse tag
            byte firstByte = data[offset++];

            var tagBytes = new List<byte> { firstByte };

            // Check if multi-byte tag
            if ((firstByte & 0x1F) == 0x1F)
            {
                while (offset < data.Length && (data[offset] & 0x80) != 0)
                {
                    tagBytes.Add(data[offset++]);
                }
                if (offset < data.Length)
                    tagBytes.Add(data[offset++]);
            }

            string tag = Convert.ToHexString(tagBytes.ToArray());

            if (offset >= data.Length)
                break;

            // Parse length
            int length = data[offset++];

            // Check if we have value bytes
            if (offset + length <= data.Length)
            {
                // Only extract if tag is in our allowed list
                if (AllowedDolTags.Contains(tag))
                {
                    for (int i = 0; i < length; i++)
                    {
                        valueList.Add(data[offset++]);
                    }
                }
                else
                {
                    // Skip this tag's value
                    offset += length;
                }
            }
            else
            {
                // Not enough bytes for value, this is DOL-only format (no values)
                return string.Empty;
            }
        }

        return valueList.Count > 0 ? Convert.ToHexString(valueList.ToArray()) : string.Empty;
    }
    /// <summary>
    /// Parses input as either DOL-only (tags) or TLV (tags with length and values).
    /// Returns (isDolOnly, extractedTagValues).
    /// </summary>
    private (bool isDolOnly, string tagValues) ParseTlvOrDol(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return (true, string.Empty);

            byte[] data = Convert.FromHexString(hex);
            var tagValueList = new List<byte>();
            int offset = 0;
            bool hasTlvData = false;

            while (offset < data.Length)
            {
                // Parse tag
                int tagStart = offset;
                byte firstByte = data[offset++];

                // Check if multi-byte tag
                if ((firstByte & 0x1F) == 0x1F)
                {
                    while (offset < data.Length && (data[offset] & 0x80) != 0)
                        offset++;
                    if (offset < data.Length)
                        offset++; // Include the last byte
                }

                if (offset >= data.Length)
                    break;

                // Parse length
                int length = data[offset++];

                // Check if we have value bytes
                if (offset + length <= data.Length)
                {
                    hasTlvData = true;
                    // Extract the value
                    for (int i = 0; i < length; i++)
                    {
                        tagValueList.Add(data[offset++]);
                    }
                }
                else
                {
                    // Not enough bytes for value, this is DOL-only format
                    return (true, string.Empty);
                }
            }

            if (hasTlvData && tagValueList.Count > 0)
            {
                return (false, Convert.ToHexString(tagValueList.ToArray()));
            }

            return (true, string.Empty);
        }

        // -----------------------------
        // Logging helper
        // -----------------------------
        private void AppendLog(string message)
        {
            if (TxtLog == null) return;
            TxtLog.Text += $"{DateTime.Now:HH:mm:ss}  {message}\n";
        }

        // -----------------------------
        // PAN change → enable Option A/B
        // -----------------------------
        private void Pan_TextChanged(object sender, TextChangedEventArgs e)
        {
            int len = TxtPan.Text.Length;

            if (Rb3Des.IsChecked == true)
            {
                Rb3DesOptionA.IsEnabled = true;
                Rb3DesOptionB.IsEnabled = true;

                if (len <= 16)
                {
                    Rb3DesOptionA.IsChecked = true;
                    Rb3DesOptionB.IsChecked = false;
                }
                else
                {
                    Rb3DesOptionA.IsChecked = false;
                    Rb3DesOptionB.IsChecked = true;
                }
            }
        }

        // -----------------------------
        // KCV Calculation
        // -----------------------------
        private void ImkAc_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateAndDisplayKcv();
        }

        private void CalculateAndDisplayKcv()
        {
            try
            {
                string imkHex = TxtImkAc.Text.Trim();

                // Check if valid hex and correct length
                if (string.IsNullOrWhiteSpace(imkHex))
                {
                    TxtImkKcv.Text = "";
                    return;
                }

                // Try to parse as hex
                byte[] imk = Convert.FromHexString(imkHex);

                string kcv;
                if (Rb3Des.IsChecked == true)
                {
                    // 3DES KCV: Encrypt 8 zero bytes, take first 3 bytes
                    if (imk.Length != 16)
                    {
                        TxtImkKcv.Text = "KCV: (IMK must be 16 bytes for 3DES)";
                        return;
                    }

                    var tdes = new SecretEmv.Core.Crypto.TripleDesEngine();
                    byte[] zeros = new byte[8];
                    byte[] encrypted = tdes.EncryptBlock(imk, zeros);
                    kcv = Convert.ToHexString(encrypted).Substring(0, 6);
                }
                else
                {
                    // AES KCV: Encrypt 16 zero bytes, take first 3 bytes
                    if (imk.Length != 16 && imk.Length != 24 && imk.Length != 32)
                    {
                        TxtImkKcv.Text = "KCV: (IMK must be 16, 24, or 32 bytes for AES)";
                        return;
                    }

                    var aes = new SecretEmv.Core.Crypto.AesEngine();
                    byte[] zeros = new byte[16];
                    byte[] encrypted = aes.EncryptBlock(imk, zeros);
                    kcv = Convert.ToHexString(encrypted).Substring(0, 6);
                }

                TxtImkKcv.Text = $"KCV: {kcv}";
            }
            catch (Exception)
            {
                TxtImkKcv.Text = "KCV: (invalid hex)";
            }
        }

        // -----------------------------
        // Block cipher selection
        // -----------------------------
        private void BlockCipher_Checked(object sender, RoutedEventArgs e)
        {
            // Recalculate KCV when cipher changes
            if (TxtImkKcv != null)
            {
                CalculateAndDisplayKcv();
            }

            if (sender is not RadioButton rbAes)
            {
                return;
            }

            // Skip if controls aren't initialized yet
            if (Rb3DesOptionA == null || Rb3DesOptionB == null || RbAesOption3 == null || AesKeyLengthPanel == null)
            {
                return;
            }

            bool isAes = rbAes.IsChecked == true;

            Rb3DesOptionA.IsEnabled = !isAes;
            Rb3DesOptionB.IsEnabled = !isAes;
            RbAesOption3.IsEnabled = isAes;

            AesKeyLengthPanel.Visibility = isAes ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AesKeyLength_Checked(object sender, RoutedEventArgs e)
        {
            AppendLog("AES key length changed.");
        }
    }
}
