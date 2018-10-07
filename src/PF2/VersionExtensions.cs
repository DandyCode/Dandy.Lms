using System;
using System.Collections.Generic;
using System.Text;

namespace Dandy.Lms.PF2
{
    /// <summary>
    /// Extensions methods for LEGO's special PF2 version format.
    /// </summary>
    public static class VersionExtensions
    {
        /// <summary>
        /// Converts a 4-byte little-endian version value that is encoded in LEGO's
        /// special format to a <see cref="Version"/>.
        /// </summary>
        /// <param name="source">The binary data source.</param>
        /// <returns>The version.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if length of <paramref name="source"/> is &lt; 4.
        /// </exception>
        /// <remarks>
        /// LEGO's terminology doesn't quite match up with .NET's terminology.
        /// LEGO considers the 3rd segment to be "bug fix" and the 4th segment
        /// to be the "build number". In .NET's terminology, the 3rd segment
        /// is called "build" and the 4th is called "revision".
        /// </remarks>

        public static Version ReadVersionLittleEndian(this ReadOnlySpan<byte> source)
        {
            if (source.Length < 4) {
                throw new ArgumentOutOfRangeException();
            }
            // LEGO seems to have taken a BCD approach to the version. The version is 4 bytes,
            // but the format is 0.0.00.0000 where each 0 is a nibble (4 bits).
            var major = source[3] >> 4;
            var minor = source[3] & 0xf;
            var build = (source[2] >> 4) * 10 + (source[2] & 0xf);
            var revision = (source[1] >> 4) * 1000 + (source[1] & 0xf) * 100 + (source[0] >> 4) * 10 | (source[0] & 0xf);
            return new Version(major, minor, build, revision);
        }

        /// <summary>
        /// Formats a version in LEGO's style for PF2 devices (i.e. <c>0.0.00.0000</c>).
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>A formatted string containing the version.</returns>
        /// <remarks>
        /// LEGO's terminology doesn't quite match up with .NET's terminology.
        /// LEGO considers the 3rd segment to be "bug fix" and the 4th segment
        /// to be the "build number". In .NET's terminology, the 3rd segment
        /// is called "build" and the 4th is called "revision".
        /// </remarks>
        public static string ToPF2Format(this Version version)
        {
            return $"{version.Major:D1}.{version.Minor:D1}.{version.Build:D2}.{version.Revision:D4}";
        }
    }
}
