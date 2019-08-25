// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/
//
// See individual classes for algorithm copyrights

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Threading.Tasks;

namespace StereogramAlgorithm
{
    public enum Algorithm { HOROPTIC, TECHMIND, CONSTRAINT_SATISFACTION, LOOKBACK, TYLER_CHANG };

    public enum Oversample { X1, X2, X3, X4, X6, X8 };
}


namespace Stereogrammer.Model
{
    using Algorithm = StereogramAlgorithm.Algorithm;
    using Oversample = StereogramAlgorithm.Oversample;

    /// <summary>
    /// Base class for stereogram generator - does most of the work, but lets subclasses provide
    /// an implementation of a particular algorithm for processing each line of the image
    /// </summary>
    public abstract class StereogramGenerator
    {
        /// <summary>
        /// Factory method to get a generator for the appropriate algorithm
        /// </summary>
        public static StereogramGenerator Get( Options options )
        {
            switch ( options.algorithm )
            {
                case Algorithm.HOROPTIC:
                    return new StereogramGeneratorHoroptic( options );
                case Algorithm.TYLER_CHANG:
                    return new StereogramGeneratorTylerChang(options);
                case Algorithm.CONSTRAINT_SATISFACTION:
                    return new StereogramGeneratorConstraintSatisfaction( options );
                case Algorithm.LOOKBACK:
                    return new StereogramGeneratorLookBack( options );
                case Algorithm.TECHMIND:
                    return new StereogramGeneratorTechmind( options );
                default:
                    throw new Exception( "Unimplemented Algorithm!" );
            }
        }

        /// <summary>
        /// Constructor caches the important options and does some common setup work
        /// </summary>
        public StereogramGenerator( Options options )
        {
            this.options = new Options( options );

            depthmap = options.depthmap;
            texture = options.texture;
            resolutionX = options.resolutionX;
            resolutionY = options.resolutionY;
            separation = options.separation;
            FieldDepth = options.FieldDepth;

            // Validate the options
            if ( depthmap == null )
                throw new ArgumentNullException( "options.depthmap" );

            // If no texture selected, use grey dots
            // If random dots are required, generate a new texture of the appropriate resolution
            if ( texture == null || texture.Type == Texture.TextureType.GREYDOTS )
            {
                texture = new TextureGreyDots( (int)separation, (int)resolutionY );
            }
            else if ( options.texture.Type == Texture.TextureType.COLOURDOTS )
            {
                texture = new TextureColourDots( (int)separation, (int)resolutionY );
            }

            // Make sure we respect aspect ratio preservation
            if ( options.bPreserveAspectRatio )
            {
                double w = (double)resolutionX;
                double h = (double)resolutionY;

                double ratio = w / h;

                double bmRatio = (double)depthmap.PixelWidth / depthmap.PixelHeight;

                if ( bmRatio < ratio )
                {
                    resolutionY = (int)h;
                    resolutionX = (int)( h * bmRatio );
                }
                else
                {
                    resolutionX = (int)w;
                    resolutionY = (int)( w / bmRatio );
                }
            }

            // Convert oversample enum to int
            switch ( options.oversample )
            {
                case Oversample.X1:
                    oversample = 1;
                    break;
                case Oversample.X2:
                    oversample = 2;
                    break;
                case Oversample.X3:
                    oversample = 3;
                    break;
                case Oversample.X4:
                    oversample = 4;
                    break;
                case Oversample.X6:
                    oversample = 6;
                    break;
                case Oversample.X8:
                    oversample = 8;
                    break;
                default:
                    throw new ArgumentException( String.Format( "Invalid oversample value: {0}", options.oversample.ToString() ) );
            }

            // Bound the depth between 0 and 1... could arguably use higher depth factors on a low-level depthmap, but more danger of a crash from invalid Zs.
            if ( FieldDepth < 0.0 )
                FieldDepth = 0.0;

            if ( FieldDepth > 1.0 )
                FieldDepth = 1.0;

            // Cache some intermediaries
            lineWidth = resolutionX;
            rows = resolutionY;
            depthWidth = lineWidth;
            depthScale = oversample;

            textureWidth = (int)separation;
            textureHeight = (int)( ( separation * texture.Height ) / texture.Width );

            // Apply oversampling factor to relevant settings
            if ( oversample > 1 )
            {
                separation *= oversample;
                lineWidth *= oversample;
                textureWidth *= oversample;

                if ( options.bInterpolateDepthmap )
                {
                    depthWidth *= oversample;
                    depthScale = 1;
                }
            }

            midpoint = lineWidth / 2;

            bytesPerPixel = sizeof( UInt32 );       // Ugh... relies on Pbgra32 pixel format being 32 bits, which obviously it will be, but it's not exactly pretty is it?
            bytesPerRow = lineWidth * bytesPerPixel;
        }

        // For thread safety, can't let the subclasses access these...
        // Pre-allocate massive buffers to hold the data instead :-/
        private Depthmap depthmap;
        private Texture texture;
        private BitmapSource bmDepthMap;
        private BitmapSource bmTexture;
        private WriteableBitmap wbStereogram;

        private Options options;

        protected bool bRemoveHiddenSurfaces { get { return options.bRemoveHiddenSurfaces; } }
        protected bool bAddConvergenceDots { get { return options.bAddConvergenceDots; } }
        protected int oversample { get ; set; }

        // Cache the important options as readonly, in the hope it helps with optimisation
        protected readonly int resolutionX;
        protected readonly int resolutionY;
        protected readonly double FieldDepth;
        protected readonly double separation;

        // Worker variables for generation algorithms...
        protected readonly int rows;
        protected readonly int lineWidth;
        protected readonly int midpoint;
        protected readonly int textureWidth;
        protected readonly int textureHeight;
        protected readonly int bytesPerPixel;
        protected readonly int bytesPerRow;

        // Depthwidth = linewidth if we're interpolating depths... subclasses don't need to know that though
        private readonly int depthWidth;
        private readonly int depthScale;

        // Big buffers to hold data... wasteful but necessary if parallelised
        UInt32[] pixels = null;
        UInt32[] texturePixels = null;
        byte[] depthBytes = null;

        // Temp hack for progress report & abort
        public int GeneratedLines { get; private set; }
        public int NumLines { get { return rows; } }
        public bool abort = false;

        /// <summary>
        /// Each algorithm operates on a line at a time, so subclasses must implement
        /// the DoLine function with algorithm appropriate functionality
        /// </summary>
        /// <param name="y"></param>
        protected abstract void DoLine( int y );

        /// <summary>
        /// Optional finaliser for the algorithm when all lines processed
        /// </summary>
        protected virtual void Finalise()
        {
        }

        /// Let's see how well C# optimises these... would hope they get inlined at least
        protected byte GetDepth( int x, int y )
        {
            return depthBytes[ (y * depthWidth) + (x / depthScale) ];
        }

        protected float GetDepthFloat( int x, int y )
        {
            return (float)( depthBytes[ (y * depthWidth) + (x / depthScale) ] ) / 255;
        }

        protected UInt32 GetTexturePixel( int x, int y )
        {
            int tp = ( ( ( y % textureHeight ) * textureWidth ) + ( ( x + midpoint ) % textureWidth ) );
            return texturePixels[ tp ];
        }

        protected UInt32 GetStereoPixel( int x, int y )
        {
            int sp = ( ( y * lineWidth ) + x );
            return pixels[ sp ];
        }

        protected void SetStereoPixel( int x, int y, UInt32 pixel )
        {
            int sp = ( ( y * lineWidth ) + x );
            pixels[ sp ] = pixel;
        }

        // Just for readability
        protected static double SquareOf( double x )
        {
            return x * x;
        }

        // Return which of two values is furthest from a mid-point (or indeed any point)
        protected static int Outermost( int a, int b, int midpoint )
        {
            return ( Math.Abs( midpoint - a ) > Math.Abs( midpoint - b ) ) ? a : b;
        }

        /// <summary>
        /// Helper to calculate stereo separation in pixels of a point at depth Z
        /// </summary>
        /// <param name="Z"></param>
        /// <returns></returns>
        protected double sep( double Z )
        {
            if ( Z < 0.0 ) Z = 0.0;
            if ( Z > 1.0 ) Z = 1.0;
            return ( ( 1 - FieldDepth * Z ) * ( 2 * separation ) / ( 2 - FieldDepth * Z ) );
        }

        /// <summary>
        /// Generate the stereogram.  Does all the common functionality, then calls the delegate
        /// set by the subclass to do the actual work.
        /// </summary>
        public Stereogram Generate()
        {
            // Let's do some profiling
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            // Convert texture to RGB24 and scale it to fit the separation (preserving ratio but doubling width for HQ mode)
            bmTexture = texture.GetToScaleAndFormat( textureWidth, textureHeight, PixelFormats.Pbgra32 );

            // Resize the depthmap to our target resolution
            bmDepthMap = depthmap.GetToScale( depthWidth, resolutionY );

            // Create a great big 2D array to hold the bytes - wasteful but convenient
            // ... and necessary for parallelisation
            pixels = new UInt32[ lineWidth * rows ];

            // Copy the texture data into a buffer
            texturePixels = new UInt32[ textureWidth * textureHeight ];
            bmTexture.CopyPixels( new Int32Rect( 0, 0, textureWidth, textureHeight ), texturePixels, textureWidth * bytesPerPixel, 0 );

            // Copy the depthmap data into a buffer
            depthBytes = new byte[ depthWidth * rows ];
            bmDepthMap.CopyPixels( new Int32Rect( 0, 0, depthWidth, rows ), depthBytes, depthWidth, 0 );

            // Can mock up a progress indicator
            GeneratedLines = 0;

            // Prime candidate for Parallel.For... yes, about doubles the speed of generation on my Quad-Core
            if ( System.Diagnostics.Debugger.IsAttached )   // Don't run parallel when debugging
            {
                for ( int y = 0; y < rows; y++ )
                {
                    DoLine( y );
                    if ( y > GeneratedLines )
                    {
                        GeneratedLines = y;
                    }
                }
            }
            else
            {
                Parallel.For( 0, rows, y =>
                {
                    if ( false == abort )
                    {
                        DoLine( y );
                    }
                    if ( y > GeneratedLines )
                    {
                        GeneratedLines = y;
                    }
                } );
            }

            if ( abort )
            {
                return null;
            }

            // Virtual finaliser... not needed for any current algorithms
            Finalise();

            // Create a writeable bitmap to dump the stereogram into
            wbStereogram = new WriteableBitmap( lineWidth, resolutionY, 96.0, 96.0, bmTexture.Format, bmTexture.Palette );
            wbStereogram.WritePixels( new Int32Rect( 0, 0, lineWidth, rows ), pixels, lineWidth * bytesPerPixel, 0 );

            BitmapSource bmStereogram = wbStereogram;

            // High quality images need to be scaled back down... 
            if ( oversample > 1 )
            {
                double over = (double)oversample;
                double centre = lineWidth / 2;
                while ( over > 1 )
                {
                    // Scale by steps... could do it in one pass, but quality would depend on what the hardware does?
                    double div = Math.Min( over, 2.0 );
//                    double div = over;
                    ScaleTransform scale = new ScaleTransform( 1.0 / div, 1.0, centre, 0 );
                    bmStereogram = new TransformedBitmap( bmStereogram, scale );
                    over /= div;
                    centre /= div;
                }
            }

            if ( bAddConvergenceDots )
            {
                // Because I made these fields read-only, I can't now restore them... 'spose I could add the dots at hi-res but I'd still need to account for the stretching
                double sep = separation / oversample;
                double mid = midpoint / oversample;

                RenderTargetBitmap rtStereogram = new RenderTargetBitmap( bmStereogram.PixelWidth, bmStereogram.PixelHeight, 96.0, 96.0, PixelFormats.Pbgra32 );

                DrawingVisual dots = new DrawingVisual();
                DrawingContext dc = dots.RenderOpen();
                dc.DrawImage( bmStereogram, new Rect( 0.0, 0.0, rtStereogram.Width, rtStereogram.Height ) );
                dc.DrawEllipse( new SolidColorBrush( Colors.Black ),
                    new Pen( new SolidColorBrush( Color.FromArgb( 128, 0, 0, 0 ) ), 1.0 ),
                    new Point( mid - sep / 2, rtStereogram.Height / 16 ), sep / 16, sep / 16 );
                dc.DrawEllipse( new SolidColorBrush( Colors.Black ),
                    new Pen( new SolidColorBrush( Color.FromArgb( 128, 0, 0, 0 ) ), 1.0 ),
                    new Point( mid + sep / 2, rtStereogram.Height / 16 ), sep / 16, sep / 16 );
                dc.Close();

                rtStereogram.Render( dots );

                bmStereogram = rtStereogram;
            }

            // Freeze the bitmap so it can be passed to other threads
            bmStereogram.Freeze();

            timer.Stop();

            Stereogram stereogram = new Stereogram( bmStereogram );
            stereogram.options = this.options;
            stereogram.Name = String.Format( "{0}+{1}+{2}", depthmap.Name, texture.Name, options.algorithm.ToString() );
            stereogram.Milliseconds = timer.ElapsedMilliseconds;
            return stereogram;
        }


    }


    /// <summary>
    /// Generate a stereogram using the Horoptic algorithm.
    /// Algorithm Copyright 1996-2012 Simon Booth
    /// http://machinewrapped.wordpress.com/stereogrammer/
    /// </summary>
    class StereogramGeneratorHoroptic : StereogramGenerator
    {
        protected int[] centreOut;

        int hidden = 0;

        public StereogramGeneratorHoroptic( Options options )
            : base( options )
        {
            // Create an array of offsets which alternate pixels from the center out to the edges
            centreOut = new int[ lineWidth ];
            int offset = midpoint;
            int flip = -1;
            for ( int i = 0; i < lineWidth; i++ )
            {
                centreOut[ i ] = offset;
                offset += ( ( i + 1 ) * flip );
                flip = -flip;
            }
        }

        protected override void DoLine( int y )
        {
            // Set up a constraints buffer with each pixel initially constrained to equal itself (probably slower than a for loop but easier to step over :p)
            // And convert depths to floats normalised 0..1
            int[] constraints = new int[ lineWidth ];
            float[] depthLine = new float[ lineWidth ];
            float max_depth = 0.0f;

            for ( int i = 0; i < lineWidth; i++ )
            {
                constraints[ i ] = i;
                depthLine[ i ] = GetDepthFloat( i, y );
            }

            // Process the line updating any constrained pixels
            for ( int ii = 0; ii < lineWidth; ii++ )
            {
                // Work from centre out
                int i = centreOut[ ii ];

                // Calculate Z value of the horopter at this x,y. w.r.t its centre.  20 * separation is approximation of distance to viewer's eyes
                double ZH = Math.Sqrt( SquareOf( 20 * separation ) - SquareOf( i - midpoint ) );

                // Scale to the range [0,1] and adjust to displacement from the far plane
                ZH = 1.0 - ( ZH / ( 20 * separation ) );

                // Separation of pixels on image plane for this point
                // Note - divide ZH by FieldDepth as the horopter is independant
                // of field depth, but sep macro is not.
                int s = (int)Math.Round( sep( depthLine[ i ] - ( ZH / FieldDepth ) ) );

                int left = i - ( s / 2 );           // The pixel on the image plane for the left eye
                int right = left + s;           // And for the right eye

                if ( ( 0 <= left ) && ( right < lineWidth ) )                     // If both points lie within the image bounds ...
                {
                    bool visible = true;
                    if ( bRemoveHiddenSurfaces )                   // Perform hidden surface test (if requested)
                    {
                        int t = 1;
                        double zt = depthLine[ i ];
                        double delta = 2 * ( 2 - FieldDepth * depthLine[ i ] ) / ( FieldDepth * separation * 2 );   // slope of line of sight
                        do
                        {
                            zt += delta;
                            visible = ( depthLine[ i - t ] < zt ) && ( depthLine[ i + t ] < zt );           // False if obscured on left or right (can only be obscured by innermost one)
                            t++;
                        }
                        while ( visible && zt < max_depth );  // cache the max depth of the line to minimise checks needed
                    }
                    if ( visible )
                    {
                        // Decide whether we want to constrain the left or right pixel
                        // Want to avoid constraint loops, so always constrain outermost pixel to innermost
                        // Should depend if one or the other is already constrained I suppose
                        int constrainee = Outermost( left, right, midpoint );
                        int constrainer = ( constrainee == left ) ? right : left;

                        // Find an unconstrained pixel and constrain ourselves to it
                        // Uh-oh, what happens if they become constrained to each other?  Constrainee is flagged as unconstrained, I suppose
                        while ( constraints[ constrainer ] != constrainer )
                            constrainer = constraints[ constrainer ];

                        constraints[ constrainee ] = constrainer;
                    }
                    else
                    {
                        hidden++;
                    }

                    // Points can only be hidden by a point closer to the centre, i.e. one we've already processed
                    if ( depthLine[ i ] > max_depth )
                        max_depth = depthLine[ i ];
                }
            }

            // Now actually set the pixels
            for ( int i = 0; i < lineWidth; i++ )
            {
                int pix = i;

                // Find an unconstrained pixel
                while ( constraints[ pix ] != pix )
                    pix = constraints[ pix ];

                // And get the RGBs from the tiled texture at that point
                SetStereoPixel( i, y, GetTexturePixel( pix, y ) );
            }
        }

        protected override void Finalise()
        {
//            Console.WriteLine( "Hidden: {0}", hidden );
        }
    }


    /// <summary>
    /// Generate a stereogram using the Constraint Satisfaction algorithm
    /// Copyright 1993 I. H. Witten, S. Inglis and H. W. Thimbleby
    /// http://www.cs.waikato.ac.nz/pubs/wp/1993/#9302
    /// </summary>
    class StereogramGeneratorConstraintSatisfaction : StereogramGenerator
    {
        public StereogramGeneratorConstraintSatisfaction( Options options )
            : base( options )
        {
        }

        /// <summary>
        /// Process a line of the image using the constraint satisfaction algorithm
        /// </summary>
        protected override void DoLine( int y )
        {
            throw new NotImplementedException("Removed from Open Source version");
        }
    }

    /// <summary>
    /// Generate a stereogram using the Lookback algorithm
    /// Copyright 1979 Christopher Tyler & Maureen Clarke
    /// </summary>
    class StereogramGeneratorLookBack : StereogramGenerator
    {
        public StereogramGeneratorLookBack( Options options )
            : base( options )
        {
        }

        protected override void DoLine( int y )
        {
            throw new NotImplementedException("Removed from Open Source version");
        }
    }

    /// <summary>
    /// Generate a stereogram using the Tyler-Chang algorithm
    /// Copyright unknown, circa 1977 Christopher Tyler & J.J. Chang
    /// </summary>
    class StereogramGeneratorTylerChang : StereogramGenerator
    {
        public StereogramGeneratorTylerChang( Options options )
            : base( options )
        {
        }

        protected override void DoLine(int y)
        {
            throw new NotImplementedException("Removed from Open Source version");
        }
    }

    /// <summary>
    /// Techmind algorith for stereogram generation, 
    /// Copyright 1995-2001 Andrew Steer.
    /// http://www.techmind.org/stereo/stech.html
    /// </summary>
    class StereogramGeneratorTechmind : StereogramGenerator
    {
        public StereogramGeneratorTechmind( Options options )
            : base( options )
        {
        }

        protected override void DoLine( int y )
        {
            throw new NotImplementedException("Removed from Open Source version");
        }
    }

}
