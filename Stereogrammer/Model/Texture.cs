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
    public class TextureCollection : BitmapCollection
    {
        public TextureCollection()
            : base( new Func<BitmapSource, BitmapType>( bmp => new Texture( bmp ) ) )
        {
        }
    }

    public class Texture : BitmapType
    {
        public enum TextureType
        {
            GREYDOTS,
            COLOURDOTS,
            BITMAP
        }

        TextureType type;

        // Create a texture for writing... nah, use the subclasses or bitmap constructor please
        protected Texture( TextureType type, int resX, int resY )
        {
            this.type = type;
            switch (type)
            {
                case TextureType.GREYDOTS:
                    Bitmap = GenerateRandomDots( resX, resY );
                    break;
                case TextureType.COLOURDOTS:
                    Bitmap = GenerateColouredDots( resX, resY );
                    break;
                case TextureType.BITMAP:
                default:
                    Bitmap = null;
                    break;
            }
        }

        public Texture( BitmapSource source )
        {
            this.type = TextureType.BITMAP;
            Bitmap = source;
        }

        public int Width { get { return PixelWidth; } }
        public int Height { get { return PixelHeight; } }

        public TextureType Type { get { return type; } }

        /// <summary>
        /// Return a Bitmap resized to the specified resolution and converted to the specified pixel format
        /// </summary>
        /// <param name="resolutionX"></param>
        /// <param name="resolutionY"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public BitmapSource GetToScaleAndFormat( int resolutionX, int resolutionY, PixelFormat format )
        {
            FormatConvertedBitmap fmTexture = new FormatConvertedBitmap( Bitmap, format, null, 0.0 );
            if ( fmTexture.PixelWidth == resolutionX && fmTexture.PixelHeight == resolutionY )
            {
                return fmTexture;
            }
            else
            {
                ScaleTransform scale = new ScaleTransform( (double)resolutionX / PixelWidth, (double)resolutionY / PixelHeight, PixelWidth / 2, PixelHeight / 2 );
                return new TransformedBitmap( fmTexture, scale );
            }
        }

        /// <summary>
        /// Generate a monochromatic random dot texture (using 256 shades of grey)
        /// </summary>
        /// <param name="resX"></param>
        /// <param name="resY"></param>
        /// <returns></returns>
        protected BitmapSource GenerateRandomDots( int resX, int resY )
        {
            Random random = new Random();

            int bytesPerRow = ( ( resX + 3 ) >> 2 ) << 2;   // Round up to multiple of 4

            byte[] pixels = new byte[ bytesPerRow * resY ];

            // Worth parallelization?
            for ( int i = 0; i < bytesPerRow * resY; i++ )
            {
                pixels[ i ] = (byte)random.Next( 256 );
            } 

            WriteableBitmap wb = new WriteableBitmap( resX, resY, 96.0, 96.0, PixelFormats.Gray8, null );
            wb.WritePixels( new System.Windows.Int32Rect( 0, 0, resX, resY ), pixels, bytesPerRow, 0 );
            return wb;
        }

        /// <summary>
        /// Generate a coloured random dot texture (using 8 bits per pixel RGB)
        /// </summary>
        /// <param name="resX"></param>
        /// <param name="resY"></param>
        /// <returns></returns>
        protected BitmapSource GenerateColouredDots( int resX, int resY )
        {
            Random random = new Random();

            int bytesPerRow = resX * 3;   // Rgb24

            byte[] pixels = new byte[ bytesPerRow * resY ];

            // Worth parallelization?
            for ( int i = 0; i < bytesPerRow * resY; i++ )
            {
                pixels[ i ] = (byte)random.Next( 256 );
            } 

            WriteableBitmap wb = new WriteableBitmap( resX, resY, 96.0, 96.0, PixelFormats.Rgb24, null );
            wb.WritePixels( new System.Windows.Int32Rect( 0, 0, resX, resY ), pixels, bytesPerRow, 0 );
            return wb;
        }
    }

    /// <summary>
    /// Subclassing mainly for the sake of updating the thumbnails... probably not the cleanest solution
    /// </summary>
    class TextureGreyDots : Texture
    {
        public TextureGreyDots( int resX, int resY )
            : base( TextureType.GREYDOTS, resX, resY )
        {
        }

        public override BitmapSource GetThumbnail( bool bSelected )
        {
            if ( bSelected )
            {
                Bitmap = GenerateRandomDots( Bitmap.PixelWidth, Bitmap.PixelHeight );                
            }
            return Bitmap;
        }
    }

    class TextureColourDots : Texture
    {
        public TextureColourDots( int resX, int resY )
            : base( TextureType.COLOURDOTS, resX, resY )
        {
        }

        public override BitmapSource GetThumbnail( bool bSelected )
        {
            if ( bSelected )
	        {
                Bitmap = GenerateColouredDots( Bitmap.PixelWidth, Bitmap.PixelHeight );
	        }
            return Bitmap;
        }
    }


}
