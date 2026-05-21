using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using nstwcsLib;

namespace RsHicReaderLib
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
        // ── Personal ──────────────────────────────────────────────────────────
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ParentName { get; set; }
        public string Sex { get; set; }
        public string PersonalNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // ── Address ───────────────────────────────────────────────────────────
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string HouseLetter { get; set; }
        public string Floor { get; set; }
        public string ApartmentNumber { get; set; }
        public string City { get; set; }
        public string Municipality { get; set; }
        public string Country { get; set; }
        public string FullAddress { get; set; }

        // ── Document ──────────────────────────────────────────────────────────
        public string CardNumber { get; set; }
        public DateTime? IssuedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string IssuingAuthority { get; set; }

        // ── Insurance ─────────────────────────────────────────────────────────
        public string InsuranceBasis { get; set; }
        public string EmployerName { get; set; }
        public string EmployerAddress { get; set; }
        public string EmployerIdNumber { get; set; }
        public string ObligeeName { get; set; }
        public string ObligeeIdNumber { get; set; }
        public DateTime? InsuranceStart { get; set; }
        public DateTime? InsuranceEnd { get; set; }

        // ── Raw blocks – direktan pristup ako neko polje nije mapirano ────────
        public ZKFixedPersoBlock RawFixed { get; set; }
        public ZKVariablePersoBlock RawVariable { get; set; }
        public ZKDocumentBlock RawDocument { get; set; }
        public ZKVariableAdminBlock RawAdmin { get; set; }

        // ── Computed ──────────────────────────────────────────────────────────
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string DateOfBirthFormatted => DateOfBirth?.ToString("dd.MM.yyyy.") ?? string.Empty;
        public string IssuedDateFormatted => IssuedDate?.ToString("dd.MM.yyyy.") ?? string.Empty;
        public string ExpiryDateFormatted => ExpiryDate?.ToString("dd.MM.yyyy.") ?? string.Empty;
        public string InsuranceStartFormatted => InsuranceStart?.ToString("dd.MM.yyyy.") ?? string.Empty;
        public string InsuranceEndFormatted => InsuranceEnd?.ToString("dd.MM.yyyy.") ?? string.Empty;
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

        // ── Jedini javni metod koji forma koristi ─────────────────────────────

        public RsHicData Read()
        {
            EnsureAlive();

            var readers = _smartCardService.ListReaders();
            if (readers == null || readers.Length == 0)
                throw new ReaderNotFoundException();

            try
            {
                _docReadService.InitCard(readers[0], 1);
            }
            catch
            {
                throw new CardNotFoundException();
            }

            try
            {
                var f = _docReadService.ReadZKFixedPersoData();
                var v = _docReadService.ReadZKVariablePersoData();
                var d = _docReadService.ReadZKDocumentData();
                var a = _docReadService.ReadZKVariableAdminData();

                return Map(f, v, d, a);
            }
            catch (CardNotFoundException) { throw; }
            catch (ReaderNotFoundException) { throw; }
            catch (Exception ex)
            {
                throw new CardReadException($"Failed to read card data: {ex.Message}", ex);
            }
            finally
            {
                try { _docReadService.ReleaseCard(); } catch { }
            }
        }

        // ── Static async helper ───────────────────────────────────────────────

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

        // ── Mapiranje nstwcsLib blokova na RsHicData ──────────────────────────

        private static RsHicData Map(
            ZKFixedPersoBlock f,
            ZKVariablePersoBlock v,
            ZKDocumentBlock d,
            ZKVariableAdminBlock a)
        {
            return new RsHicData
            {
                FirstName = S(f?.FirstNameLatin),
                LastName = S(f?.LastNameLatin),
                ParentName = S(f?.ParentNameLatin),
                Sex = S(f?.Sex),
                PersonalNumber = S(f?.PersonalNumber),
                DateOfBirth = f?.DateOfBirth,

                Street = S(v?.StreetLatin),
                HouseNumber = S(v?.HouseNumber),
                HouseLetter = S(v?.HouseLetter),
                Floor = S(v?.Floor),
                ApartmentNumber = S(v?.ApartmentNumber),
                City = S(v?.PlaceLatin),
                Municipality = S(v?.MunicipalityLatin),
                Country = S(v?.StateLatin),
                FullAddress = S(v?.AddressLatin),

                CardNumber = S(d?.DocumentSerial),
                IssuedDate = d?.IssuingDate,
                ExpiryDate = d?.ExpiryDate,
                IssuingAuthority = S(d?.IssuingAuthority),

                InsuranceBasis = S(a?.InsuranceBasis),
                EmployerName = S(a?.EmployerNameLatin),
                EmployerAddress = S(a?.EmployerAddressLatin),
                EmployerIdNumber = S(a?.EmployerIdNumber),
                ObligeeName = S(a?.ObligeeNameLatin),
                ObligeeIdNumber = S(a?.ObligeeIdNumber),
                InsuranceStart = a?.InsuranceStartDate,
                InsuranceEnd = a?.InsuranceEndDate,

                RawFixed = f,
                RawVariable = v,
                RawDocument = d,
                RawAdmin = a,
            };
        }

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