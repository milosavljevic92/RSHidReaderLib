using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using nstwcsLib;

namespace RSHidReaderLib
{
    public class CardNotFoundException : Exception
    {
        public CardNotFoundException() : base("Health card not found in reader.") { }
    }

    public class ReaderNotFoundException : Exception
    {
        public ReaderNotFoundException() : base("Smart card reader not found or not connected.") { }
    }

    public class CardReadException : Exception
    {
        public CardReadException(string message) : base(message) { }
        public CardReadException(string message, Exception inner) : base(message, inner) { }
    }

    public class RsHicData
    {
        // ── Personal  (ZKFixedPersoBlock + ZKVariableAdminBlock) ─────────────
        public string FirstName { get; set; }   // FirstNameLatin
        public string LastName { get; set; }   // LastNameLatin
        public string ParentName { get; set; }   // ParentNameLatin
        public int Gender { get; set; }   // 0 = male, 1 = female
        public string PersonalNumber { get; set; }   // IDNumber (JMBG)
        public string InsurantNumber { get; set; }   // InsurantNumber
        public DateTime? DateOfBirth { get; set; }   // DateOfBirth

        // ── Address  (ZKVariableAdminBlock) ───────────────────────────────────
        public string Street { get; set; }   // Street
        public string HouseNumber { get; set; }   // Number
        public string Entrance { get; set; }   // Entrance
        public string Apartment { get; set; }   // Apartment
        public string PostNumber { get; set; }   // PostNumber
        public string Municipality { get; set; }   // Municipality
        public string City { get; set; }   // Place
        public string Country { get; set; }   // Country
        public string CountryCode { get; set; }   // CountryCode

        // ── Document  (ZKDocumentBlock) ───────────────────────────────────────
        public string CardID { get; set; }   // CardID
        public string InsurerName { get; set; }   // InsurerName
        public string InsurerID { get; set; }   // InsurerID
        public DateTime? DateOfIssue { get; set; }   // DateOfIssue
        public DateTime? DateOfExpiry { get; set; }   // DateOfExpiry
        public string ChipSerialNumber { get; set; }   // ChipSerialNumber
        public bool IsPermanent { get; set; }   // Permanent (ZKVariablePersoBlock)

        // ── Insurance  (ZKVariableAdminBlock) ─────────────────────────────────
        public string InsuranceBasis { get; set; }   // InsuranceBasisRZZO
        public string InsuranceBasisCode { get; set; }   // InsuranceBasisRZZOCode
        public DateTime? InsuredFrom { get; set; }   // InsuredFrom
        public string RZZORegistrationNumber { get; set; }   // RZZOUserRegistrationNumber
        public string InsurerBranch { get; set; }   // InsurerBranch
        public string InsurerOffice { get; set; }   // InsurerOffice
        public string BookletIssuerCode { get; set; }   // BookletIssuerCode
        public string ParticipationFreeBecause { get; set; }   // ParticipationFreeBecause

        // ── Carrier  (ZKVariableAdminBlock) ───────────────────────────────────
        public string CarrierFirstName { get; set; }   // CarrierFirstNameLatin
        public string CarrierLastName { get; set; }   // CarrierLastNameLatin
        public string CarrierRelationship { get; set; }   // CarrierRelationship
        public string CarrierIdNumber { get; set; }   // CarrierIdNumber
        public string CarrierInsurantNumber { get; set; }   // CarrierInsurantNumber

        // ── Taxpayer  (ZKVariableAdminBlock) ──────────────────────────────────
        public string TaxpayerName { get; set; }   // TaxpayerName
        public string TaxpayerResidence { get; set; }   // TaxpayerResidence
        public string TaxpayerNumber { get; set; }   // TaxpayerNumber
        public string TaxpayerIdNumber { get; set; }   // TaxpayerIdNumber
        public string TaxpayerActivityCode { get; set; }   // TaxpayerActivityCode

        public ZKFixedPersoBlock RawFixed { get; set; }
        public ZKVariablePersoBlock RawVariable { get; set; }
        public ZKDocumentBlock RawDocument { get; set; }
        public ZKVariableAdminBlock RawAdmin { get; set; }

        // ── Computed ──────────────────────────────────────────────────────────
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string GenderLabel => Gender == 1 ? "Female" : "Male";

        public string DateOfBirthFormatted => DateOfBirth?.ToString("dd.MM.yyyy.") ?? string.Empty;
        public string DateOfIssueFormatted => DateOfIssue?.ToString("dd.MM.yyyy.") ?? string.Empty;
        public string DateOfExpiryFormatted => DateOfExpiry?.ToString("dd.MM.yyyy.") ?? string.Empty;
        public string InsuredFromFormatted => InsuredFrom?.ToString("dd.MM.yyyy.") ?? string.Empty;
    }

    public sealed class RsHicReader : IDisposable
    {
        private SmartCardService _smartCardService;
        private DocReadService _docReadService;
        private bool _disposed;

        public RsHicReader()
        {
            _smartCardService = new SmartCardService();
            _docReadService = new DocReadService();
        }

        public RsHicData Read()
        {
            EnsureAlive();

            var readers = _smartCardService.ListReaders();
            if (readers == null || readers.Length == 0)
                throw new ReaderNotFoundException();

            try { _docReadService.InitCard(readers[0], 1); }
            catch { throw new CardNotFoundException(); }

            try
            {
                ZKFixedPersoBlock f = null;
                ZKVariablePersoBlock v = null;
                ZKDocumentBlock d = null;
                ZKVariableAdminBlock a = null;

                try { f = _docReadService.ReadZKFixedPersoData(); } catch (Exception ex) { Console.WriteLine($"FixedPerso FAILED: {ex.Message}"); }
                try { v = _docReadService.ReadZKVariablePersoData(); } catch (Exception ex) { Console.WriteLine($"VariablePerso FAILED: {ex.Message}"); }
                try { d = _docReadService.ReadZKDocumentData(); } catch (Exception ex) { Console.WriteLine($"Document FAILED: {ex.Message}"); }
                try { a = _docReadService.ReadZKVariableAdminData(); } catch (Exception ex) { Console.WriteLine($"VariableAdmin FAILED: {ex.Message}"); }

                return Map(f, v, d, a);
            }
            finally
            {
                try { _docReadService.ReleaseCard(); } catch { }
            }
        }

        public static Task<RsHicData> ReadAsync()
        {
            var tcs = new TaskCompletionSource<RsHicData>();
            var thread = new Thread(() =>
            {
                try
                {
                    using (var r = new RsHicReader())
                        tcs.SetResult(r.Read());
                }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            return tcs.Task;
        }

        public static bool HasReader()
        {
            try
            {
                var readers = new SmartCardService().ListReaders();
                return readers != null && readers.Length > 0;
            }
            catch { return false; }
        }

        public static bool HasCard() => WinSCard.HasCard();

        public void Reset()
        {
            try { _docReadService.ReleaseCard(); } catch { }
            _docReadService = new DocReadService();
        }

        private static RsHicData Map(
            ZKFixedPersoBlock f,
            ZKVariablePersoBlock v,
            ZKDocumentBlock d,
            ZKVariableAdminBlock a)
        {
            return new RsHicData
            {
                FirstName = G(() => Latin(S(f?.FirstNameLatin))),
                LastName = G(() => Latin(S(f?.LastNameLatin))),
                InsurantNumber = G(() => S(f?.InsurantNumber)),
                DateOfBirth = G(() => f?.DateOfBirth),

                ParentName = G(() => a?.ParentNameLatinSpecified == true ? Latin(S(a.ParentNameLatin)) : string.Empty),
                Gender = G(() => a?.Gender ?? 0),
                PersonalNumber = G(() => S(a?.IDNumber)),

                Street = G(() => Latin(S(a?.Street))),
                HouseNumber = G(() => S(a?.Number)),
                Entrance = G(() => a?.EntranceSpecified == true ? S(a.Entrance) : string.Empty),
                Apartment = G(() => a?.ApartmentSpecified == true ? S(a.Apartment) : string.Empty),
                PostNumber = G(() => a?.PostNumberSpecified == true ? S(a.PostNumber) : string.Empty),
                Municipality = G(() => Latin(S(a?.Municipality))),
                City = G(() => Latin(S(a?.Place))),
                Country = G(() => Latin(S(a?.Country))),
                CountryCode = G(() => S(a?.CountryCode)),

                CardID = G(() => S(d?.CardID)),
                InsurerName = G(() => Latin(S(d?.InsurerName))),
                InsurerID = G(() => S(d?.InsurerID)),
                DateOfIssue = G(() => d?.DateOfIssue),
                DateOfExpiry = G(() => d?.DateOfExpiry),
                ChipSerialNumber = G(() => S(d?.ChipSerialNumber)),
                IsPermanent = G(() => (v?.Permanent ?? 0) == 1),

                InsuranceBasis = G(() => Latin(S(a?.InsuranceBasisRZZO))),
                InsuranceBasisCode = G(() => S(a?.InsuranceBasisRZZOCode)),
                InsuredFrom = G(() => a?.InsuredFrom),
                RZZORegistrationNumber = G(() => S(a?.RZZOUserRegistrationNumber)),
                InsurerBranch = G(() => Latin(S(a?.InsurerBranch))),
                InsurerOffice = G(() => a?.InsurerOfficeSpecified == true ? Latin(S(a.InsurerOffice)) : string.Empty),
                BookletIssuerCode = G(() => S(a?.BookletIssuerCode)),
                ParticipationFreeBecause = G(() => a?.ParticipationFreeBecauseSpecified == true ? Latin(S(a.ParticipationFreeBecause)) : string.Empty),

                CarrierFirstName = G(() => a?.CarrierFirstNameLatinSpecified == true ? Latin(S(a.CarrierFirstNameLatin)) : string.Empty),
                CarrierLastName = G(() => a?.CarrierLastNameLatinSpecified == true ? Latin(S(a.CarrierLastNameLatin)) : string.Empty),
                CarrierRelationship = G(() => a?.CarrierRelationshipSpecified == true ? Latin(S(a.CarrierRelationship)) : string.Empty),
                CarrierIdNumber = G(() => a?.CarrierIdNumberSpecified == true ? S(a.CarrierIdNumber) : string.Empty),
                CarrierInsurantNumber = G(() => a?.CarrierInsurantNumberSpecified == true ? S(a.CarrierInsurantNumber) : string.Empty),

                TaxpayerName = G(() => Latin(S(a?.TaxpayerName))),
                TaxpayerResidence = G(() => Latin(S(a?.TaxpayerResidence))),
                TaxpayerNumber = G(() => a?.TaxpayerNumberSpecified == true ? S(a.TaxpayerNumber) : string.Empty),
                TaxpayerIdNumber = G(() => a?.TaxpayerIdNumberSpecified == true ? S(a.TaxpayerIdNumber) : string.Empty),
                TaxpayerActivityCode = G(() => a?.TaxpayerActivityCodeSpecified == true ? S(a.TaxpayerActivityCode) : string.Empty),

                RawFixed = f,
                RawVariable = v,
                RawDocument = d,
                RawAdmin = a,
            };
        }

        private static T G<T>(Func<T> getter)
        {
            try { return getter(); }
            catch { return default; }
        }

        public static class SerbianScript
        {
            private static readonly (string Cyr, string Lat)[] _map =
            {
                ("Љ","Lj"), ("Њ","Nj"), ("Џ","Dž"),
                ("љ","lj"), ("њ","nj"), ("џ","dž"),
                ("А","A"),  ("Б","B"),  ("В","V"),  ("Г","G"),
                ("Д","D"),  ("Ђ","Đ"),  ("Е","E"),  ("Ж","Ž"),
                ("З","Z"),  ("И","I"),  ("Ј","J"),  ("К","K"),
                ("Л","L"),  ("М","M"),  ("Н","N"),  ("О","O"),
                ("П","P"),  ("Р","R"),  ("С","S"),  ("Т","T"),
                ("Ћ","Ć"),  ("У","U"),  ("Ф","F"),  ("Х","H"),
                ("Ц","C"),  ("Ч","Č"),  ("Ш","Š"),
                ("а","a"),  ("б","b"),  ("в","v"),  ("г","g"),
                ("д","d"),  ("ђ","đ"),  ("е","e"),  ("ж","ž"),
                ("з","z"),  ("и","i"),  ("ј","j"),  ("к","k"),
                ("л","l"),  ("м","m"),  ("н","n"),  ("о","o"),
                ("п","p"),  ("р","r"),  ("с","s"),  ("т","t"),
                ("ћ","ć"),  ("у","u"),  ("ф","f"),  ("х","h"),
                ("ц","c"),  ("ч","č"),  ("ш","š"),
            };

            public static bool IsCyrillic(string s)
            {
                if (string.IsNullOrEmpty(s)) return false;
                foreach (char c in s)
                    if (c >= '\u0400' && c <= '\u04FF') return true;
                return false;
            }

            public static string ToLatin(string s)
            {
                if (string.IsNullOrEmpty(s)) return s;
                var sb = new StringBuilder(s);
                foreach (var (cyr, lat) in _map)
                    sb.Replace(cyr, lat);
                return sb.ToString();
            }

            public static string EnsureLatin(string s) =>
                IsCyrillic(s) ? ToLatin(s) : s ?? string.Empty;
        }
        private static string Latin(string s) => SerbianScript.EnsureLatin(s);

        private static string S(object o) => o?.ToString()?.Trim() ?? string.Empty;

        private void EnsureAlive()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RsHicReader));
        }

        public void Dispose()
        {
            if (_disposed) return;
            try { _docReadService.ReleaseCard(); } catch { }
            _disposed = true;
        }
    }

    internal static class WinSCard
    {
        private const uint SCARD_SCOPE_USER = 0;
        private const uint SCARD_SHARE_SHARED = 2;
        private const uint SCARD_PROTOCOL_Tx = 3;
        private const uint SCARD_LEAVE_CARD = 0;

        [DllImport("winscard.dll")]
        static extern int SCardEstablishContext(uint scope, IntPtr r1, IntPtr r2, out IntPtr ctx);

        [DllImport("winscard.dll")]
        static extern int SCardReleaseContext(IntPtr ctx);

        [DllImport("winscard.dll")]
        static extern int SCardListReadersA(IntPtr ctx, string groups, byte[] buf, ref int len);

        [DllImport("winscard.dll")]
        static extern int SCardConnectA(IntPtr ctx, string reader, uint share, uint protocol,
            out IntPtr card, out uint activeProtocol);

        [DllImport("winscard.dll")]
        static extern int SCardDisconnect(IntPtr card, uint disposition);

        public static bool HasCard()
        {
            if (SCardEstablishContext(SCARD_SCOPE_USER, IntPtr.Zero, IntPtr.Zero, out var ctx) != 0)
                return false;
            try
            {
                int len = 0;
                if (SCardListReadersA(ctx, null, null, ref len) != 0 || len <= 2) return false;
                var buf = new byte[len];
                SCardListReadersA(ctx, null, buf, ref len);
                string reader = Encoding.Default.GetString(buf).Split('\0')[0];
                if (string.IsNullOrEmpty(reader)) return false;
                int ret = SCardConnectA(ctx, reader, SCARD_SHARE_SHARED, SCARD_PROTOCOL_Tx,
                    out IntPtr card, out uint _);
                if (ret == 0) { SCardDisconnect(card, SCARD_LEAVE_CARD); return true; }
                return false;
            }
            finally { SCardReleaseContext(ctx); }
        }
    }
}