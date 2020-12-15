//sources: 
//http://csharpexamples.com/tag/parallel-bitmap-processing/
//
/*
GOALS:
render mandelbrot                                   -Y
render julia                                        -Y
create animations (zooming or between sets)?        -n
(store the img in tmp dir)                          -n
make it so you can set it a desktop background      -n
GUI:                                                -n
    choose coordinates 
    set as desktop background
    save somewhere on pc 
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;

namespace fractalBackground
{
    class Program
    {
        static void Main(string[] args){
            int screenWidth  = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            ImageRender myScreen = new ImageRender(screenWidth*1, screenHeight*1);
            myScreen.drawFractals();
        }
    }

    class Fractal
    {
        
        //class properties
        public double screenHeight { get; set; }
        public double screenWidth { get; set; } 
        public double xmax {get; set;}
        public double xmin {get; set;}
        public double ymax {get; set;}
        public double ymin {get; set;}


        //default constructor
        public Fractal(){}

        //Instancce constructor
        public Fractal(double width, double height){
            screenWidth    = width; 
            screenHeight   = height;
        }

        public double[] xy_conv(double x, double y){
            double[] result = new double[2];
            result[0] =  ((this.xmax-this.xmin)/(this.screenWidth)*x)+this.xmin;
            result[1] =  ((this.ymax-this.ymin)/(this.screenHeight)*y)+this.ymin;
            return result;
        }
        
        public void correctCoordinates(){
            if(screenWidth ==screenHeight){Console.WriteLine("did not have to correct\n"); return;}
            double shortestSide = screenHeight>screenWidth?screenWidth:screenHeight;
            double longestSide = screenHeight>screenWidth?screenHeight:screenWidth;

            double pixel_ratio = screenHeight/screenWidth;
            double plane_ratio = (ymax + Math.Abs(ymin))/(xmax+Math.Abs(xmin));

            double pixelToPlane_ratio = (ymax + Math.Abs(ymin))/shortestSide;
            double amountToAdd = (longestSide-shortestSide) * pixelToPlane_ratio;
            Console.WriteLine("pixeltoplaneratio: {0}\nAmounttoadd: {1}\npixelratio: {2}\nScreenwidth/height: {3} / {4}", pixelToPlane_ratio, amountToAdd, pixel_ratio, screenWidth, screenHeight);

            if(pixel_ratio > 1){
                this.ymax += amountToAdd/2;
                this.ymin -= amountToAdd/2;
            }
            if(pixel_ratio < 1){
                this.xmax += amountToAdd/2;
                this.xmin -= amountToAdd/2;
            }
            Console.WriteLine("did correct: \n{0} {1} {2} {3}", ymax, ymin, xmax, xmin);
        }

        public delegate double[] isInBounds(double x, double y, int bail);

        public void drawFractal(int width, int height, Bitmap btmp, isInBounds inBounds){
            unsafe{
                BitmapData btmpD =  btmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, btmp.PixelFormat);
                int bytesPerPixel = Bitmap.GetPixelFormatSize(btmp.PixelFormat) / 8;
                byte* ptrFirstPixel = (byte*)btmpD.Scan0;
                int bail = 400;

                Parallel.For(0, height, y=>{ 
                    byte* currentLine = ptrFirstPixel + (y * (width*bytesPerPixel));
                    for(int x = 0; x < width; x++){
                        double[] coor = xy_conv(x, y);
                        double[] ab = inBounds(coor[0], coor[1], bail);
                        currentLine[x * bytesPerPixel] = Convert.ToByte(255-Math.Round(255*(ab[2]/bail)));
                        currentLine[x * bytesPerPixel+1] = Convert.ToByte(Math.Round(255*(1-ab[2]/bail)));
                        currentLine[x * bytesPerPixel+2] = Convert.ToByte(Math.Round(255*(ab[2]/bail)));
                        currentLine[x * bytesPerPixel+3] = 255;
                    }
                });
                btmp.UnlockBits(btmpD);
            }
        }
    }

    #region MandelbrotSet
    class MandelFractal : Fractal
    {
        public MandelFractal(double width, double height){ 
            screenWidth    = width; 
            screenHeight   = height;
            this.xmax =  0.5; 
            this.xmin = -2.1; 
            this.ymax =  1.3; 
            this.ymin = -1.3; 
        }

        public double[] isInMandelSet(double x, double y, int bail)
        {
            double[] result = new double[3];
            double cur = 0; 
            double z = 0; 
            double a = x; 
            double b = y;
            while(cur < bail && Math.Abs(z) <= 2){
                double xtemp = a; 
                double ytemp = b; 
                a = xtemp*xtemp - ytemp*ytemp + x;
                b = xtemp*ytemp*2 + y;
                z = a + b;
                cur++;
            }
            result [0] = a; 
            result [1] = b; 
            result [2] = cur; 
            return result; 
        }

        public void drawMandel(int width, int height, Bitmap btmp){
        //loop through each pixel
            this.correctCoordinates();
            //Console.WriteLine("hello, let's see if we are dealing w a square {0} {1} {2} {3}\n", this.xmax, this.xmin, this.ymax, this.ymin); 
            //Console.WriteLine("width and height: {0} {1}", width, height);
            drawFractal(width, height, btmp, isInMandelSet);
        }  
    }
    #endregion

    #region JuliaSet
    class JuliaFractal : Fractal
    {
        double r {get; set;}
        double cx {get; set;}
        double cy {get; set;}

        public JuliaFractal(int width, int height, double rx, double iy){
            screenWidth    = width; 
            screenHeight   = height;
            cx = rx; 
            cy = iy;
            r = initializeBounds(cx, cy); 
        }

        double initializeBounds(double real, double imaginary){
            int i = imaginary > 0? -1 : 1; 
            double c = (real*real + imaginary*imaginary); 
            c = Math.Sqrt(c) * 4 + 1;
            double r = (1 + Math.Sqrt(c))/2; 
            Console.WriteLine("bounds made!: {0}", r); 
            
            this.xmin = -r; 
            this.ymin = -r;
            this.xmax =  r; 
            this.ymax =  r; 
            return r; 
        }

        double[] isInJuliaSet(double x, double y, int bail){
            double[] result = new double[3];
            double cur = 0; 
            double z = 0; 
            double a = x; 
            double b = y;
            while(cur < bail && Math.Abs(z) < r){
                double xtemp = a; 
                double ytemp = b; 
                a = xtemp*xtemp - ytemp*ytemp + cx;
                b = xtemp*ytemp*2 + cy;
                z = a + b;
                cur++;
            }
            result [0] = a; 
            result [1] = b; 
            result [2] = cur; 
            return result;  
        }

        public void drawJulia(int width, int height, Bitmap btmp){
            this.correctCoordinates();
            drawFractal(width, height, btmp, isInJuliaSet);
        }   
    }
    #endregion

    class ImageRender
    {
        //defaut class intializer 
        public ImageRender(){ }
        
        //class initializer pt.2
        public ImageRender(int width, int height){
            screenWidth    = width; 
            screenHeight   = height;
        }
        //class properties
        public int screenWidth { get; set; }
        public int screenHeight { get; set; }

        public void drawFractals(){
            
            MandelFractal mandelFractal = new MandelFractal(screenWidth, screenHeight); 
            Bitmap bitmap1 = new Bitmap(screenWidth, screenHeight);
            mandelFractal.drawMandel(screenWidth, screenHeight, bitmap1);
            bitmap1.Save(@"mandelBackground.png");
            
            // -0.74543, 0.11301
            //  -0.8, 0.156
            // 0.285, 0.01
            // -0.0085, 0.71
            JuliaFractal juliaFractal = new JuliaFractal(screenWidth, screenHeight, -0.74543, 0.11301); 
            Bitmap bitmap2 = new Bitmap(screenWidth, screenHeight);
            juliaFractal.drawJulia(screenWidth, screenHeight, bitmap2);
            bitmap2.Save(@"juliaBackground.png");
        }
    }
}

