using System;
using System.Threading.Tasks;
using RSHidReaderLib;

namespace RsHicConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (!RsHicReader.HasReader())
            {
                Console.WriteLine("[ERROR] No card reader detected. Please connect a USB card reader.");
                Exit();
                return;
            }

            if (!RsHicReader.HasCard())
            {
                Console.WriteLine("[ERROR] No card inserted. Please insert your health card into the reader.");
                Exit();
                return;
            }

            Console.WriteLine("Reading health card...");

            try
            {
                RsHicData card = await RsHicReader.ReadAsync();

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  PERSONAL DATA");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Full name          : {card.FullName}");
                Console.WriteLine($"First name         : {card.FirstName}");
                Console.WriteLine($"Last name          : {card.LastName}");
                Console.WriteLine($"Parent name        : {card.ParentName}");
                Console.WriteLine($"Gender             : {card.GenderLabel}");
                Console.WriteLine($"Personal number    : {card.PersonalNumber}");
                Console.WriteLine($"Insurant number    : {card.InsurantNumber}");
                Console.WriteLine($"Date of birth      : {card.DateOfBirthFormatted}");

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  ADDRESS");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Street             : {card.Street} {card.HouseNumber}");
                Console.WriteLine($"Entrance / Apt     : {card.Entrance} / {card.Apartment}");
                Console.WriteLine($"Post number        : {card.PostNumber}");
                Console.WriteLine($"City               : {card.City}");
                Console.WriteLine($"Municipality       : {card.Municipality}");
                Console.WriteLine($"Country            : {card.Country} ({card.CountryCode})");

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  DOCUMENT");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Card ID            : {card.CardID}");
                Console.WriteLine($"Insurer name       : {card.InsurerName}");
                Console.WriteLine($"Insurer ID         : {card.InsurerID}");
                Console.WriteLine($"Date of issue      : {card.DateOfIssueFormatted}");
                Console.WriteLine($"Date of expiry     : {card.DateOfExpiryFormatted}");
                Console.WriteLine($"Chip serial        : {card.ChipSerialNumber}");
                Console.WriteLine($"Permanent          : {card.IsPermanent}");

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  INSURANCE");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Insurance basis    : {card.InsuranceBasis}");
                Console.WriteLine($"Basis code         : {card.InsuranceBasisCode}");
                Console.WriteLine($"Insured from       : {card.InsuredFromFormatted}");
                Console.WriteLine($"RZZO reg. number   : {card.RZZORegistrationNumber}");
                Console.WriteLine($"Insurer branch     : {card.InsurerBranch}");
                Console.WriteLine($"Insurer office     : {card.InsurerOffice}");
                Console.WriteLine($"Booklet issuer code: {card.BookletIssuerCode}");
                Console.WriteLine($"Participation free : {card.ParticipationFreeBecause}");

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  CARRIER");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Carrier name       : {card.CarrierFirstName} {card.CarrierLastName}");
                Console.WriteLine($"Carrier relation   : {card.CarrierRelationship}");
                Console.WriteLine($"Carrier ID         : {card.CarrierIdNumber}");
                Console.WriteLine($"Carrier insurant   : {card.CarrierInsurantNumber}");

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  TAXPAYER");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Taxpayer name      : {card.TaxpayerName}");
                Console.WriteLine($"Taxpayer residence : {card.TaxpayerResidence}");
                Console.WriteLine($"Taxpayer number    : {card.TaxpayerNumber}");
                Console.WriteLine($"Taxpayer ID        : {card.TaxpayerIdNumber}");
                Console.WriteLine($"Activity code      : {card.TaxpayerActivityCode}");
            }
            catch (CardNotFoundException ex)
            {
                Console.WriteLine($"[ERROR] Card not found: {ex.Message}");
            }
            catch (ReaderNotFoundException ex)
            {
                Console.WriteLine($"[ERROR] Reader not available: {ex.Message}");
            }
            catch (CardReadException ex)
            {
                Console.WriteLine($"[ERROR] Read error: {ex.Message}");
            }

            Exit();
        }

        static void Exit()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}