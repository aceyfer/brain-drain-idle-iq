using Plankton.Editor.Files;

namespace Plankton.Editor
{
    public class FileManager
    {
        public PlatformFiles_Android android = new PlatformFiles_Android();
        public PlatformFiles_iOS ios = new PlatformFiles_iOS();

        public virtual void SaveFiles(bool generateAar)
        {
#if UNITY_IOS
            ios.SaveFiles(generateAar);
#elif UNITY_ANDROID
            android.SaveFiles(generateAar);
#endif
        }
    }
}