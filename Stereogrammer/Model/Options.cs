// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Stereogrammer.Model
{
    using Algorithm = StereogramAlgorithm.Algorithm;
    using Oversample = StereogramAlgorithm.Oversample;

    /// <summary>
    /// Wrap up the options for stereogram generation for easy sharing and serialization
    /// </summary>
    public class Options
    {
        public int resolutionX { get; set; }
        public int resolutionY { get; set; }
        public double separation { get; set; }
        public double FieldDepth { get; set; }

        public bool bRemoveHiddenSurfaces { get; set; }
        public bool bAddConvergenceDots { get; set; }
        public bool bPreserveAspectRatio { get; set; }
        public bool bInterpolateDepthmap { get; set; }

        public Algorithm algorithm { get; set; }
        public Oversample oversample { get; set; }

        public Depthmap depthmap { get; set; }
        public Texture texture { get; set; }

        public DateTime time = System.DateTime.Now;

        public Options()
        {
            resolutionX = 1024;
            resolutionY = 768;
            separation = 128.0;
            FieldDepth = 0.3333;
            bRemoveHiddenSurfaces = false;
            bAddConvergenceDots = false;
            bPreserveAspectRatio = true;
            bInterpolateDepthmap = true;
            oversample = Oversample.X2;
        }

        public Options( Options options )
        {
            resolutionX = options.resolutionX;
            resolutionY = options.resolutionY;
            separation = options.separation;
            FieldDepth = options.FieldDepth;
            oversample = options.oversample;
            bRemoveHiddenSurfaces = options.bRemoveHiddenSurfaces;
            bAddConvergenceDots = options.bAddConvergenceDots;
            bPreserveAspectRatio = options.bPreserveAspectRatio;
            bInterpolateDepthmap = options.bInterpolateDepthmap;
            algorithm = options.algorithm;
            depthmap = options.depthmap;
            texture = options.texture;
        }

    }

}
