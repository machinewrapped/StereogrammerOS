// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;


namespace Stereogrammer.Model
{
    public enum FileType { JPG, BMP, PNG };

    public class BitmapType
    {
        BitmapSource _bitmap = null;
        public int PixelWidth = 0;
        public int PixelHeight = 0;
        public bool bCanRemove = true;

        public BitmapSource Bitmap 
        {
            get { return _bitmap; }
            protected set
            {
                _bitmap = value;
                PixelWidth = _bitmap.PixelWidth;
                PixelHeight = _bitmap.PixelHeight;
                _bitmap.Freeze();
            }
        }

        public string Name { get; set; }
        public string filename = null;

        public virtual BitmapSource GetThumbnail( bool bSelected )
        {
            return Bitmap;
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Encode and write an image to a file
        /// </summary>
        /// <param name="stereogram"></param>
        /// <param name="filename"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public void SaveImage( string filename, FileType type )
        {
            this.Name = System.IO.Path.GetFileNameWithoutExtension( filename );
            this.filename = filename;

            BitmapEncoder encoder = null;
            switch ( type )
            {
                case FileType.JPG:
                    encoder = new JpegBitmapEncoder() { QualityLevel = 100 };
                    break;
                case FileType.BMP:
                    encoder = new BmpBitmapEncoder();
                    break;
                case FileType.PNG:
                    encoder = new PngBitmapEncoder();
                    break;
                default:
                    throw new Exception( "Error saving file" );
            }

            BitmapSource writeme = new FormatConvertedBitmap( Bitmap, PixelFormats.Bgra32, null, 0.0f );

            encoder.Frames.Add( BitmapFrame.Create( writeme ) );

            using ( var stream = new FileStream( filename, FileMode.Create ) )
            {
                encoder.Save( stream );
            }
        }
    }
}
