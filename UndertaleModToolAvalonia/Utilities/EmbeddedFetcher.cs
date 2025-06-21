using AAYInvisionaryTTSPlayer.Utilities;
using System.IO;

namespace UndertaleModToolAvalonia.Utilities
{
    public static class EmbeddedFetcher
    {
        public static byte[] ExtractResource(string filename)
        {
            using (Stream resFilestream = AssetGrabber.GetAssetStream(filename))
            {
                if (resFilestream == null) return new byte[4] { 0, 0, 0, 0 };
                byte[] ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
                return ba;
            }
        }
    }
}
