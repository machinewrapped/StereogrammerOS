// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace Stereogrammer.Model
{
    using Algorithm = StereogramAlgorithm.Algorithm;

    /// <summary>
    /// Specialised BitmapCollection (basically provides a custom factory func)
    /// </summary>
    public class StereogramCollection : BitmapCollection
    {
        public StereogramCollection()
            : base( new Func<BitmapSource, BitmapType>( bmp => new Stereogram( bmp ) ) )
        {
        }
    }

    /// <summary>
    /// Stereogram image.  A BitmapType which also stores the options it was generated from.
    /// </summary>
    public class Stereogram : BitmapType
    {
        public Stereogram( BitmapSource bitmap )
        {
            Bitmap = bitmap;
        }

        public bool HasOptions { get { return options != null; } }

        public DateTime GenerationTime { get { return options != null ? options.time : System.DateTime.Now; } }

        /// <summary>
        /// Options used to generate the stereogram
        /// </summary>
        public Options options = null;

        /// <summary>
        /// Time taken to generate stereogram, in milliseconds
        /// </summary>
        public long Milliseconds = 0;
    }


}
