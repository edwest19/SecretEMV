// Copyright (c) 2026 edwest19
// All rights reserved.
// This file was generated using Microsoft Copilot.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SecretEmv.Core.Emv;
using SecretEmv.Core.Models;
using System;
using System.Linq;

namespace SecretEmv.GenAC
{
    public sealed partial class AcGenPage : Page
    {
        private readonly EmvCryptoPipelineService _pipeline = new EmvCryptoPipelineService();

        public AcGenPage()
        {
            this.InitializeComponent();
            
            // Set default values
            Rb3Des.IsChecked = true;
            Rb3DesOptionA.IsChecked = true;
            RbAes128.IsChecked = true;
        }

        /// <summary>
        /// Cleans hex input by removing spaces, line breaks, tabs, and hyphens.
        /// Allows users to paste formatted hex like "00 00 00" or "00-00-00".
        /// </summary>
        private static string CleanHexInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return new string(input
                .Where(c => !char.IsWhiteSpace(c) && c != '-')
                .ToArray())
                .ToUpperInvariant();
        }

        /// <summary>
        /// Validates that a string contains only valid hex characters.
        /// </summary>
        private static bool IsValidHex(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            return input.All(c => (c >= '0' && c <= '9') || 
                                  (c >= 'A' && c <= 'F') || 
                                  (c >= 'a' && c <= 'f'));
        }

        private void BlockCipher_Checked(object sender, RoutedEventArgs e)
        {
            if (Rb3Des == null || RbAes == null)
                return;

            bool is3Des = Rb3Des.IsChecked == true;

            Rb3DesOptionA.IsEnabled = is3Des;
            Rb3DesOptionB.IsEnabled = is3Des;
            RbAesOption3.IsEnabled = !is3Des;

            if (AesKeyLengthPanel != null)
                AesKeyLengthPanel.Visibility = is3Des ? Visibility.Collapsed : Visibility.Visible;

            if (is3Des)
            {
                Rb3DesOptionA.IsChecked = true;
            }
            else
            {
                RbAesOption3.IsChecked = true;
            }
        }

        private void AesKeyLength_Checked(object sender, RoutedEventArgs e)
        {
            // AES key length selection handled by radio buttons
        }

        private void ImkAc_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string cleaned = CleanHexInput(TxtImkAc.Text);
                
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    TxtImkKcv.Text = "";
                    return;
                }

                if (!IsValidHex(cleaned))
                {
                    TxtImkKcv.Text = "Invalid hex characters";
                    TxtImkKcv.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                    return;
                }

                byte[] keyBytes = Convert.FromHexString(cleaned);
                
                // Calculate KCV (Key Check Value) - first 3 bytes of encrypting zero block
                if (keyBytes.Length == 16 || keyBytes.Length == 24)
                {
                    var tdes = new SecretEmv.Core.Crypto.TripleDesEngine();
                    byte[] zeroBlock = new byte[8];
                    byte[] encrypted = tdes.EncryptBlock(keyBytes, zeroBlock);
                    string kcv = Convert.ToHexString(encrypted[..3]);
                    TxtImkKcv.Text = $"KCV: {kcv}";
                    TxtImkKcv.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                }
                else
                {
                    TxtImkKcv.Text = $"Key length: {keyBytes.Length} bytes (expected 16 or 24)";
                    TxtImkKcv.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);
                }
            }
            catch
            {
                TxtImkKcv.Text = "Invalid hex format";
                TxtImkKcv.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }

        private void Pan_TextChanged(object sender, TextChangedEventArgs e)
        {
            string pan = CleanHexInput(TxtPan.Text);
            
            if (string.IsNullOrWhiteSpace(pan))
                return;

            // Auto-select Option A or B based on PAN length
            if (Rb3Des.IsChecked == true)
            {
                if (pan.Length <= 16)
                {
                    Rb3DesOptionA.IsChecked = true;
                }
                else
                {
                    Rb3DesOptionB.IsChecked = true;
                }
            }
        }

        private void GenerateCardMasterKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string imkHex = CleanHexInput(TxtImkAc.Text);
                string pan = CleanHexInput(TxtPan.Text);
                string psn = CleanHexInput(TxtPsn.Text);

                if (string.IsNullOrWhiteSpace(imkHex) || string.IsNullOrWhiteSpace(pan) || string.IsNullOrWhiteSpace(psn))
                {
                    AppendLog("ERROR: IMK-AC, PAN, and PSN are required.");
                    return;
                }

                byte[] imk = Convert.FromHexString(imkHex);
                byte[] cmk;

                if (Rb3Des.IsChecked == true)
                {
                    if (Rb3DesOptionA.IsChecked == true)
                    {
                        cmk = _pipeline.DeriveDesIccMasterKeyOptionA(imk, pan, psn);
                        AppendLog("Card Master Key generated (3DES Option A).");
                    }
                    else
                    {
                        cmk = _pipeline.DeriveDesIccMasterKeyOptionB(imk, pan, psn);
                        AppendLog("Card Master Key generated (3DES Option B).");
                    }
                }
                else
                {
                    cmk = _pipeline.DeriveAesIccMasterKey(imk, pan, psn);
                    AppendLog("Card Master Key generated (AES).");
                }

                TxtCardMasterKey.Text = Convert.ToHexString(cmk);
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR: {ex.Message}");
            }
        }

        private void GenerateSessionKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string cmkHex = CleanHexInput(TxtCardMasterKey.Text);
                string atcHex = CleanHexInput(TxtAtc.Text);

                if (string.IsNullOrWhiteSpace(cmkHex) || string.IsNullOrWhiteSpace(atcHex))
                {
                    AppendLog("ERROR: Card Master Key and ATC are required.");
                    return;
                }

                byte[] cmk = Convert.FromHexString(cmkHex);
                byte[] sessionKey;

                if (Rb3Des.IsChecked == true)
                {
                    sessionKey = _pipeline.DeriveDesSessionKey(cmk, atcHex);
                    AppendLog("Session Key generated (3DES).");
                }
                else
                {
                    sessionKey = _pipeline.DeriveAesSessionKey(cmk, atcHex);
                    AppendLog("Session Key generated (AES).");
                }

                TxtSessionKey.Text = Convert.ToHexString(sessionKey);
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR: {ex.Message}");
            }
        }

        private void GenerateArqc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string sessionKeyHex = CleanHexInput(TxtSessionKey.Text);
                string tagValuesHex = CleanHexInput(TxtTagValues.Text);

                if (string.IsNullOrWhiteSpace(sessionKeyHex) || string.IsNullOrWhiteSpace(tagValuesHex))
                {
                    AppendLog("ERROR: Session Key and Tag Values are required.");
                    return;
                }

                byte[] dolBytes = Array.Empty<byte>();
                byte[] tagValuesBytes = Convert.FromHexString(tagValuesHex);

                var result = _pipeline.GenerateArqc(sessionKeyHex, dolBytes, tagValuesBytes);

                TxtArqc.Text = result.Arqc;
                AppendLog("ARQC generated.");
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR (ARQC): {ex.Message}");
            }
        }

        private void GenerateArpc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string arqcHex = CleanHexInput(TxtArqc.Text);
                string arcHex = CleanHexInput(TxtArc.Text);
                string sessionKeyHex = CleanHexInput(TxtSessionKey.Text);

                if (string.IsNullOrWhiteSpace(arqcHex) || string.IsNullOrWhiteSpace(arcHex) || string.IsNullOrWhiteSpace(sessionKeyHex))
                {
                    AppendLog("ERROR: ARQC, ARC/CSU, and Session Key are required.");
                    return;
                }

                var input = new ArpcInput
                {
                    Arqc = arqcHex,
                    Arc = arcHex,
                    SessionKeyAc = sessionKeyHex
                };

                var result = _pipeline.GenerateArpc(input);

                TxtArpc.Text = result.Arpc;
                
                // Indicate method used
                byte[] arcBytes = Convert.FromHexString(arcHex);
                if (arcBytes.Length == 4)
                {
                    AppendLog("ARPC generated (MAC4 method with CSU).");
                }
                else
                {
                    AppendLog("ARPC generated (Method 1 with ARC).");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR (ARPC): {ex.Message}");
            }
        }

        private void TxtDol_TextChanged(object sender, TextChangedEventArgs e)
        {
            // DOL parsing logic can be added here if needed
        }

        private void AppendLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            TxtLog.Text += $"{timestamp} {message}\n";
            
            // Auto-scroll to bottom
            if (TxtLog.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Get the window that hosts this page
            var window = GetWindowForElement(this);
            window?.Close();
        }

        /// <summary>
        /// Gets the window that hosts the given element.
        /// </summary>
        private Microsoft.UI.Xaml.Window? GetWindowForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (var window in Microsoft.UI.Xaml.Window.Current.GetType()
                    .GetProperty("Windows", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?
                    .GetValue(null) as System.Collections.Generic.IEnumerable<Microsoft.UI.Xaml.Window> ?? Array.Empty<Microsoft.UI.Xaml.Window>())
                {
                    if (window.Content?.XamlRoot == element.XamlRoot)
                    {
                        return window;
                    }
                }
            }
            return null; // Add explicit return instead of falling through
        }

        /// <summary>
        /// Cleans hex input when user leaves the field.
        /// Removes spaces, line breaks, hyphens automatically.
        /// </summary>
        private void HexInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsReadOnly)
            {
                string originalText = textBox.Text;
                string cleanedText = CleanHexInput(originalText);
                
                // Only update if text actually changed to avoid unnecessary updates
                if (originalText != cleanedText)
                {
                    // Store cursor position
                    int cursorPosition = textBox.SelectionStart;
                    
                    textBox.Text = cleanedText;
                    
                    // Try to restore a sensible cursor position
                    // (for most cases, end of text makes sense after cleaning)
                    textBox.SelectionStart = Math.Min(cursorPosition, cleanedText.Length);
                }
            }
        }
    }
}
