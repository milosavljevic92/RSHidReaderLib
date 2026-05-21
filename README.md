# RsHidReader

C# library for reading Serbian health insurance cards via smart card readers.  
Reads personal data, address, document info, and insurance details from the chip.

---

## Requirements

| | |
|---|---|
| **OS** | Windows 10 / Windows 11 |
| **Framework** | .NET Framework 4.8 (Windows Desktop) |
| **Library** | nstwcsLib (included with RFZO reader software) |
| **Hardware** | PC/SC compatible smart card reader |

---

## nstwcsLib Setup

### 1. Get the library

`nstwcsLib.dll` is distributed as part of the official RFZO (Republic Fund for Health Insurance) smart card SDK.  
Add it to your project the same way as any other reference.

### 2. Add the DLL to your project

1. Right-click the project in Solution Explorer → **Add → Existing Item**
2. Browse to and select `nstwcsLib.dll`
3. Click **Add**

### 3. Set Copy to Output Directory

1. Click on `nstwcsLib.dll` in Solution Explorer
2. In the **Properties** panel find **Copy to Output Directory**
3. Set it to **Copy always**

### 4. Add as Reference

1. Right-click **References** in Solution Explorer → **Add Reference**
2. Browse to `nstwcsLib.dll` and add it

---

## Installation

Copy `RsHidReader.cs` (`RsHidReaderLib` namespace) into your project.

---

## Usage

```csharp
using RsHidReaderLib;

// Check reader and card status
if (!RsHidReader.HasReader())
{
    MessageBox.Show("Please connect a card reader to a USB port.", "Reader Not Found",
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}

if (!RsHidReader.HasCard())
{
    MessageBox.Show("Please insert your health card into the reader.", "No Card Detected",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

// Read the card (async – does not block the UI)
try
{
    RsHicData card = await RsHidReader.ReadAsync();

    txtName.Text      = card.FirstName;
    txtSurname.Text   = card.LastName;
    txtJMBG.Text      = card.PersonalNumber;
    txtBirthDate.Text = card.DateOfBirthFormatted;
    txtAddress.Text   = card.FullAddress;
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
| `FirstName` | First name |
| `LastName` | Last name |
| `ParentName` | Parent's name |
| `Sex` | Sex |
| `PersonalNumber` | JMBG (personal ID number) |
| `DateOfBirth` | Date of birth (DateTime) |
| `DateOfBirthFormatted` | Date of birth (DD.MM.YYYY.) |
| `FullName` | First name + Last name |

### Address

| Field | Description |
|---|---|
| `Street` | Street name |
| `HouseNumber` | House number |
| `HouseLetter` | House letter (A, B...) |
| `Floor` | Floor |
| `ApartmentNumber` | Apartment number |
| `City` | City |
| `Municipality` | Municipality |
| `Country` | Country |
| `FullAddress` | Full address string from chip |

### Document

| Field | Description |
|---|---|
| `CardNumber` | Health card number |
| `IssuedDate` | Issue date (DateTime) |
| `IssuedDateFormatted` | Issue date (DD.MM.YYYY.) |
| `ExpiryDate` | Expiry date (DateTime) |
| `ExpiryDateFormatted` | Expiry date (DD.MM.YYYY.) |
| `IssuingAuthority` | Issuing authority |

### Insurance

| Field | Description |
|---|---|
| `InsuranceBasis` | Basis of insurance |
| `EmployerName` | Employer name |
| `EmployerAddress` | Employer address |
| `EmployerIdNumber` | Employer ID number |
| `ObligeeName` | Obligee name |
| `ObligeeIdNumber` | Obligee ID number |
| `InsuranceStart` | Insurance start date (DateTime) |
| `InsuranceStartFormatted` | Insurance start date (DD.MM.YYYY.) |
| `InsuranceEnd` | Insurance end date (DateTime) |
| `InsuranceEndFormatted` | Insurance end date (DD.MM.YYYY.) |

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
RsHidReader.HasReader()      // bool – checks if a reader is connected
RsHidReader.HasCard()        // bool – checks if a card is inserted
RsHidReader.ReadAsync()      // Task<RsHicData> – reads all card data

// Instance usage
using (var reader = new RsHidReader())
{
    RsHicData card = reader.Read();
}

// Reset reader after an error
using (var reader = new RsHidReader())
    reader.Reset();
```

---

## Notes

- `ReadAsync` runs on a dedicated STA thread — safe for WinForms and WPF.
- `HasReader` uses nstwcsLib `SmartCardService.ListReaders()` for detection.
- `HasCard` uses the Windows WinSCard API — non-blocking.
- `RawFixed`, `RawVariable`, `RawDocument` and `RawAdmin` expose the original nstwcsLib blocks directly, useful if a field is not yet mapped in `RsHicData`.
- Call `Reset()` after a `CardReadException` to reinitialize the reader session.

---

## License

MIT
