# RSHidReader

C# library for reading Serbian health insurance cards via smart card readers.  
Reads personal data, address, document info, insurance and carrier details from the chip.

---

## Requirements

| | |
|---|---|
| **OS** | Windows 10 / Windows 11 |
| **Framework** | .NET Framework 4.8 (Windows Desktop) |
| **DLL** | nstwcs-hc-client.dll (RFZO SDK) |
| **Hardware** | PC/SC compatible smart card reader |

---

## nstwcs-hc-client.dll Setup

> `nstwcs-hc-client.dll` is a COM component and **must be registered on the system** before use.  
> This is a one-time step per machine.

### 1. Register the DLL with regsvr32

Open **Command Prompt as Administrator** and run:

```cmd
regsvr32 "C:\path\to\nstwcs-hc-client.dll"
```

A dialog will confirm successful registration:
> *DllRegisterServer in nstwcs-hc-client.dll succeeded.*

To unregister:

```cmd
regsvr32 /u "C:\path\to\nstwcs-hc-client.dll"
```

> **Note:** On 64-bit Windows, use the 64-bit version of regsvr32 located at `C:\Windows\System32\regsvr32.exe`.  
> For 32-bit DLLs on a 64-bit system, use `C:\Windows\SysWOW64\regsvr32.exe` instead.

### 2. Add the DLL to your project

1. Right-click the project in Solution Explorer → **Add → Existing Item**
2. Browse to and select `nstwcs-hc-client.dll`
3. Click **Add**

### 3. Set Copy to Output Directory

1. Click on `nstwcs-hc-client.dll` in Solution Explorer
2. In the **Properties** panel find **Copy to Output Directory**
3. Set it to **Copy always**

### 4. Add as Reference

1. Right-click **References** in Solution Explorer → **Add Reference**
2. Click **Browse** and select `nstwcs-hc-client.dll`
3. Click **OK**

---

## Installation

Copy `RsHicReader.cs` (`RSHidReaderLib` namespace) into your project.

---

## Usage

```csharp
using RSHidReaderLib;

// Check reader and card status
if (!RsHicReader.HasReader())
{
    MessageBox.Show("Please connect a card reader to a USB port.", "Reader Not Found",
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}

if (!RsHicReader.HasCard())
{
    MessageBox.Show("Please insert your health card into the reader.", "No Card Detected",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

// Read the card (async – does not block the UI)
try
{
    RsHicData card = await RsHicReader.ReadAsync();

    txtName.Text      = card.FirstName;
    txtSurname.Text   = card.LastName;
    txtJMBG.Text      = card.PersonalNumber;
    txtBirthDate.Text = card.DateOfBirthFormatted;
    txtAddress.Text   = $"{card.Street} {card.HouseNumber}, {card.City}";
}
catch (CardNotFoundException ex)
{
    MessageBox.Show(ex.Message, "Card Not Found",
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
catch (ReaderNotFoundException ex)
{
    MessageBox.Show(ex.Message, "Reader Not Available",
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
catch (CardReadException ex)
{
    MessageBox.Show(ex.Message, "Read Error",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

---

## RsHicData — Available Fields

### Personal Data

| Field | Description |
|---|---|
| `FirstName` | First name (Latin) |
| `LastName` | Last name (Latin) |
| `ParentName` | Parent's name (Latin) |
| `Gender` | Gender (0 = Male, 1 = Female) |
| `GenderLabel` | Gender as string ("Male" / "Female") |
| `PersonalNumber` | JMBG (personal ID number) |
| `InsurantNumber` | Health insurance number |
| `DateOfBirth` | Date of birth (DateTime) |
| `DateOfBirthFormatted` | Date of birth (DD.MM.YYYY.) |
| `FullName` | First name + Last name |

### Address

| Field | Description |
|---|---|
| `Street` | Street name |
| `HouseNumber` | House number |
| `Entrance` | Entrance |
| `Apartment` | Apartment number |
| `PostNumber` | Postal code |
| `City` | City |
| `Municipality` | Municipality |
| `Country` | Country |
| `CountryCode` | Country code |

### Document

| Field | Description |
|---|---|
| `CardID` | Health card ID |
| `InsurerName` | Insurer name (RFZO) |
| `InsurerID` | Insurer ID |
| `DateOfIssue` | Issue date (DateTime) |
| `DateOfIssueFormatted` | Issue date (DD.MM.YYYY.) |
| `DateOfExpiry` | Expiry date (DateTime) |
| `DateOfExpiryFormatted` | Expiry date (DD.MM.YYYY.) |
| `ChipSerialNumber` | Chip serial number |
| `IsPermanent` | Permanent card flag |

### Insurance

| Field | Description |
|---|---|
| `InsuranceBasis` | Basis of insurance (RZZO description) |
| `InsuranceBasisCode` | Basis of insurance (RZZO code) |
| `InsuredFrom` | Insurance start date (DateTime) |
| `InsuredFromFormatted` | Insurance start date (DD.MM.YYYY.) |
| `RZZORegistrationNumber` | RZZO user registration number |
| `InsurerBranch` | Insurer branch |
| `InsurerOffice` | Insurer office |
| `BookletIssuerCode` | Booklet issuer code |
| `ParticipationFreeBecause` | Reason for participation exemption |

### Carrier

| Field | Description |
|---|---|
| `CarrierFirstName` | Carrier first name (Latin) |
| `CarrierLastName` | Carrier last name (Latin) |
| `CarrierRelationship` | Relationship to carrier |
| `CarrierIdNumber` | Carrier ID number |
| `CarrierInsurantNumber` | Carrier insurant number |

### Taxpayer

| Field | Description |
|---|---|
| `TaxpayerName` | Taxpayer name |
| `TaxpayerResidence` | Taxpayer residence |
| `TaxpayerNumber` | Taxpayer number |
| `TaxpayerIdNumber` | Taxpayer ID number |
| `TaxpayerActivityCode` | Taxpayer activity code |

### Raw Blocks

Direct access to original nstwcsLib objects for any fields not mapped above.

| Field | Type |
|---|---|
| `RawFixed` | `ZKFixedPersoBlock` |
| `RawVariable` | `ZKVariablePersoBlock` |
| `RawDocument` | `ZKDocumentBlock` |
| `RawAdmin` | `ZKVariableAdminBlock` |

---

## API Reference

```csharp
// Static – no instance required
RsHicReader.HasReader()      // bool – checks if a reader is connected
RsHicReader.HasCard()        // bool – checks if a card is inserted
RsHicReader.ReadAsync()      // Task<RsHicData> – reads all card data

// Instance usage
using (var reader = new RsHicReader())
{
    RsHicData card = reader.Read();
}

// Reset reader after a CardReadException
using (var reader = new RsHicReader())
    reader.Reset();
```

---

## Notes

- All text fields are automatically transliterated from Cyrillic to Latin script.
- Optional fields protected by `*Specified` flags return empty string when not present instead of throwing a COM exception.
- `ReadAsync` runs on a dedicated STA thread — safe for WinForms and WPF.
- `HasReader` uses nstwcsLib `SmartCardService.ListReaders()` for detection.
- `HasCard` uses the Windows WinSCard API — non-blocking.
- `RawFixed`, `RawVariable`, `RawDocument` and `RawAdmin` expose the original nstwcsLib blocks directly for any unmapped fields.
- Call `Reset()` after a `CardReadException` to reinitialize the reader session.

---

## License

MIT
