using FSO.Content.Framework;
using FSO.Vitaboy;
using System.IO;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for cmx files (*.cmx). 
    /// for some reason, these are plaintext bcf.
    /// </summary>
    public class CMXCodec : IContentCodec<BCF>
    {
        #region IContentCodec<OTF> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            // A few legacy downloads store a binary BCF with a .cmx extension.
            // The known form starts with a 32-bit zero skeleton count.
            if (!stream.CanSeek)
            {
                using (var copy = new MemoryStream())
                {
                    stream.CopyTo(copy);
                    copy.Position = 0;
                    return GenDecode(copy);
                }
            }
            var start = stream.Position;
            var binary = stream.ReadByte() == 0 && stream.ReadByte() == 0 &&
                         stream.ReadByte() == 0 && stream.ReadByte() == 0;
            stream.Position = start;
            return new BCF(stream, !binary);
        }

        #endregion
    }
}
