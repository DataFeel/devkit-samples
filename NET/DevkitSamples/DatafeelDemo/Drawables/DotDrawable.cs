using Microsoft.Maui.Graphics.Platform;
using System.Diagnostics;
using System.Reflection;
using IImage = Microsoft.Maui.Graphics.IImage;
using DatafeelDemo;

namespace DatafeelDemo.Drawables
{

    internal class DotDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            IImage image = null;

            Assembly assembly = GetType().GetTypeInfo().Assembly;
            Stream stream = null;
            try
            {
                stream = assembly.GetManifestResourceStream("DatafeelDemo.Resources.Images.dot_ui_outer_ring.png");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                image = null;
            }
            if(stream != null)
            {
                using (stream)
                {
                    image = PlatformImage.FromStream(stream);
                }
            }

            if (image != null)
            {
                canvas.DrawImage(image, 10, 10, image.Width, image.Height);
            }
        }
    }
}
