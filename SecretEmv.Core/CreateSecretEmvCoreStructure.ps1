# Create folder structure for SecretEmv.Core

# Top-level folders
$folders = @(
    "Primitives",
    "Crypto",
    "Emv\MasterKeyDerivation",
    "Emv\SessionKeyDerivation",
    "Emv\Arqc",
    "Emv\Arpc",
    "Emv\Dol",
    "Models",
    "Logging"
)

foreach ($folder in $folders) {
    New-Item -ItemType Directory -Path $folder -Force
}

# Primitives files
$primitives = @(
    "ByteArrayUtils.cs",
    "XorUtils.cs",
    "PaddingUtils.cs",
    "ConcatUtils.cs",
    "DecimalisationUtils.cs",
    "BlockUtils.cs",
    "HexUtils.cs"
)

foreach ($file in $primitives) {
    New-Item -ItemType File -Path ("Primitives\" + $file) -Force
}

# Crypto files
$crypto = @(
    "TripleDesEngine.cs",
    "AesEngine.cs",
    "AesCmacEngine.cs",
    "RetailMacEngine.cs",
    "CmacSubkeyGenerator.cs",
    "CryptoConstants.cs"
)

foreach ($file in $crypto) {
    New-Item -ItemType File -Path ("Crypto\" + $file) -Force
}

# EMV files
$emvMaster = @(
    "DesMasterKeyDeriver.cs",
    "AesMasterKeyDeriver.cs",
    "PanProcessing.cs"
)

foreach ($file in $emvMaster) {
    New-Item -ItemType File -Path ("Emv\MasterKeyDerivation\" + $file) -Force
}

$emvSession = @(
    "DesSessionKeyDeriver.cs",
    "AesSessionKeyDeriver.cs",
    "DiversificationDataBuilder.cs"
)

foreach ($file in $emvSession) {
    New-Item -ItemType File -Path ("Emv\SessionKeyDerivation\" + $file) -Force
}

$emvArqc = @(
    "ArqcEngine.cs",
    "ArqcMacBuilder.cs",
    "ArqcDataFormatter.cs"
)

foreach ($file in $emvArqc) {
    New-Item -ItemType File -Path ("Emv\Arqc\" + $file) -Force
}

$emvArpc = @(
    "ArpcEngine.cs",
    "ArpcMethod1.cs",
    "ArpcMethod2.cs"
)

foreach ($file in $emvArpc) {
    New-Item -ItemType File -Path ("Emv\Arpc\" + $file) -Force
}

$emvDol = @(
    "DolParser.cs",
    "DolBuilder.cs",
    "DolElement.cs"
)

foreach ($file in $emvDol) {
    New-Item -ItemType File -Path ("Emv\Dol\" + $file) -Force
}

New-Item -ItemType File -Path "Emv\EmvConstants.cs" -Force

# Models files
$models = @(
    "MasterKeyInput.cs",
    "MasterKeyResult.cs",
    "SessionKeyInput.cs",
    "SessionKeyResult.cs",
    "ArqcInput.cs",
    "ArqcResult.cs",
    "ArpcInput.cs",
    "ArpcResult.cs"
)

foreach ($file in $models) {
    New-Item -ItemType File -Path ("Models\" + $file) -Force
}

# Logging files
$logging = @(
    "StepLog.cs",
    "LogBuilder.cs"
)

foreach ($file in $logging) {
    New-Item -ItemType File -Path ("Logging\" + $file) -Force
}

Write-Host "SecretEmv.Core folder structure created successfully."
