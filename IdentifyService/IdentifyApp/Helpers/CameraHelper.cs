using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace IdentifyApp.Helpers
{
    public class CameraHelper
    {
        public static async Task ReencodeAndSavePhotoAsync(IRandomAccessStream stream, StorageFile file)
        {
            using (var inputStream = stream)
            {
                var decoder = await BitmapDecoder.CreateAsync(inputStream);

                using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);

                    await encoder.FlushAsync();
                }
            }
        }

        public static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            // Get available devices for capturing pictures
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Get the desired camera by panel
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }
    }
}
