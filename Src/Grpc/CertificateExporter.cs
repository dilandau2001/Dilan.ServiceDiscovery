/*
 * Most of the code of this class is either copies or highly derived
 * from several sources inside StackOverflow
 * https://stackoverflow.com/
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using Grpc.Core;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    public static class CertificateExporter
    {
        /// <summary>
        /// Exports the X509 certificate as pem format string.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns></returns>
        private static string ExportX509CertificateAsPEM(this X509Certificate2 certificate)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            AppendByteArrayAsPEM(builder, certificate.Export(X509ContentType.Cert));
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }

        /// <summary>
        /// Finds the certificate by user name in Computer Personal store.
        /// </summary>
        /// <param name="certificateSubjectDistinguishedName">Name of the certificate subject distinguished.</param>
        /// <returns>Returns the first certificate found in the personal store that matches the distinguished name.
        /// Be careful this function is not taking into account if you have several records with the same name.</returns>
        public static X509Certificate2 FindCertificate(string certificateSubjectDistinguishedName)
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                try
                {
                    store.Open(OpenFlags.ReadOnly);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return null;
                }
                
                X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindByIssuerName, certificateSubjectDistinguishedName, false);
                X509Certificate2 cert = null;
                if (certCollection.Count > 0)
                {
                    cert = certCollection[0];
                }
                else
                {
                    Console.WriteLine("Certificate not found");
                }

                store.Close();
                return cert;
            }
        }

        /// <summary>
        /// Appends the byte array as pem.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="input">The input.</param>
        private static void AppendByteArrayAsPEM(StringBuilder builder, byte[] input)
        {
            string base64 = Convert.ToBase64String(input);

            for (int i = 0; i < base64.Length; i += 64)
            {
                string line = base64.Substring(i, Math.Min(64, base64.Length - i));
                builder.AppendLine(line);
            }
        }

        /// <summary>
        /// Exports the X509 private RSA key.
        /// </summary>
        /// <param name="cert">The cert.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">The selected certificate with serial '{cert.SerialNumber}' does not contain a private key.</exception>
        private static string ExportX509PrivateRSAKey(this X509Certificate2 cert)
        {
            if(!cert.HasPrivateKey)
            {
                throw new NotSupportedException($"The selected certificate with serial '{cert.SerialNumber}' does not contain a private key.");
            }

            RSA rsa = cert.GetRSAPrivateKey();

            // Known issue: Not able to pass this point when certificate comes from file.
            RSAParameters parameters = rsa.ExportParameters(true);
            StringBuilder sb = new StringBuilder();

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                
                using (MemoryStream innerStream = new MemoryStream())
                {
                    BinaryWriter innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                    EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                    EncodeIntegerBigEndian(innerWriter, parameters.D);
                    EncodeIntegerBigEndian(innerWriter, parameters.P);
                    EncodeIntegerBigEndian(innerWriter, parameters.Q);
                    EncodeIntegerBigEndian(innerWriter, parameters.DP);
                    EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                    EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                    int length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                char[] base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                string stringBase64 = new string(base64);
                sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");

                // Output as Base64 with lines chopped at 64 characters
                for (int i = 0; i < base64.Length; i += 64)
                {
                    string substring = stringBase64.Substring(i, Math.Min(64, base64.Length - i));
                    sb.AppendLine(substring);
                }
                
                sb.AppendLine("-----END RSA PRIVATE KEY-----");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Encodes the length.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="length">The length.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">length - Length must be non-negative</exception>
        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                int temp = length;
                int bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }

                stream.Write((byte)(bytesRequired | 0x80));
                for (int i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }

        /// <summary>
        /// Encodes the integer big endian.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="forceUnsigned">if set to <c>true</c> [force unsigned].</param>
        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02);
            int prefixZeros = value.TakeWhile(t => t == 0).Count();

            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }

                for (int i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }

        /// <summary>
        /// Creates the SSL server credentials.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <returns></returns>
        public static SslServerCredentials CreateSslServerCredentials(string subject)
        {
            X509Certificate2 certificate = GetServerCertificate(subject);

            KeyCertificatePair keyPair = new KeyCertificatePair(
                certificate.ExportX509CertificateAsPEM(),
                certificate.ExportX509PrivateRSAKey());

            return new SslServerCredentials(new[] {keyPair});
        }

        /// <summary>
        /// Creates the SSL server credentials.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns></returns>
        public static SslServerCredentials CreateSslServerCredentials(X509Certificate2 certificate)
        {
            KeyCertificatePair keyPair = new KeyCertificatePair(
                certificate.ExportX509CertificateAsPEM(),
                certificate.ExportX509PrivateRSAKey());

            return new SslServerCredentials(new[] {keyPair});
        }

        /// <summary>
        /// Gets the server certificate.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">No certificate matching the subject name {subject} could be found</exception>
        private static X509Certificate2 GetServerCertificate(string subject)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindBySubjectName, subject, true);
            store.Close();

            if (certificates.Count == 0)
            {
                throw new ArgumentException($"No certificate matching the subject name {subject} could be found");
            }
        
            return certificates[0];
        }
    }
}
