// Copyright (c) 2026 edwest19
// All rights reserved.
// This file was generated using Microsoft Copilot.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecretEmv.Core.Emv;
using SecretEmv.Core.Models;
using System;
using System.Text;

namespace SecretEmv.GenAC
{
    public sealed partial class AcGenPage : Page
    {
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
        // Block cipher selection
        // -----------------------------
        private void BlockCipher_Checked(object sender, RoutedEventArgs e)
        {
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
