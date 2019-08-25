// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Stereogrammer.Model
{
    /// <summary>
    /// Specialised BitmapCollection (basically provides a custom factory func)
    /// </summary>
    public class DepthmapCollection : BitmapCollection
    {
        public DepthmapCollection()
            : base( new Func<BitmapSource, BitmapType>( bmp => new Depthmap( bmp ) ) )
        {
        }
    }

    public class Depthmap : BitmapType
    {
        public Depthmap( int resX, int resY )
        {
            // Create a flat monotone depthmap
            Bitmap = new WriteableBitmap( resX, resY, 96, 96, PixelFormats.Gray2, null );
        }

        public Depthmap( BitmapSource source )
        {
            // Always convert to greyscale
            Bitmap = new FormatConvertedBitmap( source, PixelFormats.Gray8, null, 0.0 );
        }

        public BitmapSource GetToScale( int resolutionX, int resolutionY )
        {
            ScaleTransform scale = new ScaleTransform( (double)resolutionX / PixelWidth, (double)resolutionY / PixelHeight, PixelWidth / 2, PixelHeight / 2 );
            return new TransformedBitmap( Bitmap, scale );
        }

        /// <summary>
        /// Adjust the levels of a depthmap
        /// </summary>
        /// <param name="black"></param>
        /// <param name="white"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="gamma"></param>
        /// <param name="bHardBlack"></param>
        /// <returns></returns>
        public BitmapSource GetLevelAdjusted( LevelAdjustments opt )
        {
            // Well, this is retarded... can manipulate the pixel data but not the palette data?
            WriteableBitmap wb = new WriteableBitmap( Bitmap );
            byte[] pixels = new byte[ wb.PixelWidth * wb.PixelHeight ];
            wb.CopyPixels( pixels, wb.PixelWidth, 0 );

            double deltain = opt.whitein - opt.blackin;
            double deltaout = opt.whiteout - opt.blackout;

            for (int i = 0; i < wb.PixelWidth * wb.PixelHeight; i++)
            {
                double src = (double)pixels[i] / 255;
                // Set black and white points
                double dst = Math.Min( Math.Max( 0, src - opt.blackin ), deltain );
                if ( dst > 0.0 || !opt.bHardBlack )
                {
                    // Renormalise
                    dst /= deltain;
                    // Apply gamma
                    dst = Math.Pow( dst, opt.gamma );
                    // Scale to output range
                    dst = opt.blackout + ( dst * deltaout );                    
                }
                pixels[i] = (byte)(dst * 255);
            }

            wb.WritePixels( new System.Windows.Int32Rect( 0, 0, wb.PixelWidth, wb.PixelHeight ), pixels, wb.PixelWidth, 0 );
            return wb;
        }

        /// <summary>
        /// Return a bitmap with inverted levels
        /// </summary>
        /// <returns></returns>
        public BitmapSource GetLevelInverted()
        {
            // Well, this is retarded... can manipulate the pixel data but not the palette data?
            WriteableBitmap wb = new WriteableBitmap( Bitmap );
            byte[] pixels = new byte[ wb.PixelWidth * wb.PixelHeight ];
            wb.CopyPixels( pixels, wb.PixelWidth, 0 );
            for ( int i = 0; i < wb.PixelWidth * wb.PixelHeight; i++ )
            {
                pixels[ i ] = (byte)(255 - pixels[ i ]);
            }
            wb.WritePixels( new System.Windows.Int32Rect( 0, 0, wb.PixelWidth, wb.PixelHeight ), pixels, wb.PixelWidth, 0 );
            return wb;
        }

        /// <summary>
        /// Get a new depthmap which is the Z-depth max combination of the source and input depthmaps
        /// </summary>
        /// <param name="another"></param>
        /// <returns></returns>
        public Depthmap MergeWith( Depthmap another )
        {
            // First bitmap
            WriteableBitmap wb = new WriteableBitmap( Bitmap );
            byte[] pixels = new byte[ wb.PixelWidth * wb.PixelHeight ];
            wb.CopyPixels( pixels, wb.PixelWidth, 0 );

            // Second bitmap, scaled to match the first (really should take the biggest one I guess)
            BitmapSource bm2 = another.GetToScale( PixelWidth, PixelHeight );
            byte[] pixels2 = new byte[ bm2.PixelWidth * bm2.PixelHeight ];
            bm2.CopyPixels( pixels2, bm2.PixelWidth, 0 );
            for ( int i = 0; i < wb.PixelWidth * wb.PixelHeight; i++ )
            {
                if ( pixels2[i] > pixels[i] )
                {
                    pixels[ i ] = pixels2[ i ];
                }
            }
            wb.WritePixels( new System.Windows.Int32Rect( 0, 0, wb.PixelWidth, wb.PixelHeight ), pixels, wb.PixelWidth, 0 );
            return new Depthmap( wb );
        }

    }

    public struct LevelAdjustments
    {
        public double blackin;
        public double whitein;
        public double blackout;
        public double whiteout;
        public double gamma;
        public bool bHardBlack;

        public LevelAdjustments( double blackin = 0.0, double whitein = 1.0, double blackout = 0.0, double whiteout = 1.0, double gamma = 1.0, bool bHardBlack = true)
        {
            this.blackin = blackin;
            this.whitein = whitein;
            this.blackout = blackout;
            this.whiteout = whiteout;
            this.gamma = gamma;
            this.bHardBlack = bHardBlack;
        }
    }
}
