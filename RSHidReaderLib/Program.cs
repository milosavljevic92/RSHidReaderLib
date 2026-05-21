using System;
using System.Threading.Tasks;
using RsHicReaderLib;

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
                Console.WriteLine($"Full name        : {card.FullName}");
                Console.WriteLine($"First name       : {card.FirstName}");
                Console.WriteLine($"Last name        : {card.LastName}");
                Console.WriteLine($"Parent name      : {card.ParentName}");
                Console.WriteLine($"Sex              : {card.Sex}");
                Console.WriteLine($"Personal number  : {card.PersonalNumber}");
                Console.WriteLine($"Date of birth    : {card.DateOfBirthFormatted}");

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  ADDRESS");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Street           : {card.Street} {card.HouseNumber}{card.HouseLetter}");
                Console.WriteLine($"Entrance / Floor : {card.Floor}");
                Console.WriteLine($"Apartment        : {card.ApartmentNumber}");
                Console.WriteLine($"City             : {card.City}");
                Console.WriteLine($"Municipality     : {card.Municipality}");
                Console.WriteLine($"Country          : {card.Country}");
                Console.WriteLine($"Full address     : {card.FullAddress}");

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  DOCUMENT");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Card number      : {card.CardNumber}");
                Console.WriteLine($"Issued           : {card.IssuedDateFormatted}");
                Console.WriteLine($"Expires          : {card.ExpiryDateFormatted}");
                Console.WriteLine($"Issuing authority: {card.IssuingAuthority}");

                Console.WriteLine();
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine("  INSURANCE");
                Console.WriteLine("══════════════════════════════════");
                Console.WriteLine($"Insurance basis  : {card.InsuranceBasis}");
                Console.WriteLine($"Employer         : {card.EmployerName}");
                Console.WriteLine($"Employer address : {card.EmployerAddress}");
                Console.WriteLine($"Employer ID      : {card.EmployerIdNumber}");
                Console.WriteLine($"Obligee name     : {card.ObligeeName}");
                Console.WriteLine($"Obligee ID       : {card.ObligeeIdNumber}");
                Console.WriteLine($"Insurance from   : {card.InsuranceStartFormatted}");
                Console.WriteLine($"Insurance to     : {card.InsuranceEndFormatted}");
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