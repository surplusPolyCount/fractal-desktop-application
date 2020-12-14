/*
GOALS:
render mandelbrot 
render julia 
(store the img in tmp dir)
make it so you can set it a desktop background
create animations between different sets?
GUI: 
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

namespace fractalBackground
{
    class Program
    {
        static void Main(string[] args){
            int screenWidth  = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            ImageRender myScreen = new ImageRender(screenWidth*2, screenHeight*2);
            myScreen.drawFractal();
        }
    }

    class MandelFractal
    {
        public double    xmax     =  0.5; 
        public double    xmin     = -2.1; 
        public double    ymax     =  1.3; 
        public double    ymin     = -1.3; 

        public MandelFractal(){ }
        public MandelFractal(double width, double height){
            screenWidth    = width; 
            screenHeight   = height;
        }

        //class properties
        public double screenHeight { get; set; }
        public double screenWidth { get; set; }

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
                ymax += amountToAdd/2;
                ymin -= amountToAdd/2;
            }
            if(pixel_ratio < 1){
                xmax += amountToAdd/2;
                xmin -= amountToAdd/2;
            }
            Console.WriteLine("did correct: \n{0} {1} {2} {3}", ymax, ymin, xmax, xmin);
        } 
        
        double[] xy_conv(double x, double y){
            double[] result = new double[2];
            result[0] =  ((xmax-xmin)/(screenWidth)*x)+xmin;
            result[1] =  ((ymax-ymin)/(screenHeight)*y)+ymin;
            return result;
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

        public void drawMandel(double width, double height, Bitmap imageToDraw){
        //loop through each pixel
            this.correctCoordinates();
            Console.WriteLine("hello, let's see if we are dealing w a square {0} {1} {2} {3}\n", this.xmax, this.xmin, this.ymax, this.ymin); 
            Console.WriteLine("width and height: {0} {1}", width, height);
            Color pixCol = new Color(); 
            double[] data = new double[3];

            for(int y= 0; y < height; y++){
                for(int x = 0; x < width; x++){
                    int bail = 75;
                    double[] coor = xy_conv(x, y);

                    double[] ab = isInMandelSet(coor[0], coor[1], bail);
                    if(ab[0] + ab[1] <= 2){
                        //console.log(ab[0] + ab[1]);
                        data[0] = ab[2]>bail/4?Math.Round(255*(ab[2]/bail), 0):0;
                        data[1] = ab[2]>bail/4?Math.Round(255*(ab[2]/bail), 0):0;
                        data[2] = ab[2]>bail/10?Math.Round(255*(ab[2]/bail),0):0;
                    }
                    pixCol = Color.FromArgb(
                        Convert.ToByte(data[0]),
                        Convert.ToByte(data[1]),
                        Convert.ToByte(data[2]),
                        255 
                    );
                    imageToDraw.SetPixel(x,y,pixCol);
                }
            }
        }  
    }

    class JuliaFractal{
        public JuliaFractal(){ }
        public JuliaFractal(int width, int height, double rx, double iy){
            screenWidth    = width; 
            screenHeight   = height;
            cx = rx; 
            cy = iy;
            r = initializeBounds(cx, cy); 
        }
        int screenWidth { get; set; }
        int screenHeight { get; set; }
        double r {get; set;}
        double cx {get; set;}
        double cy {get; set;}
        double xmin;
        double ymin; 
        double xmax; 
        double ymax;  

        double initializeBounds(double real, double imaginary){
            int i = imaginary > 0? -1 : 1; 
            double c = (real*real + imaginary*imaginary); 
            c = Math.Sqrt(c) * 4 + 1;
            double r = (1 + Math.Sqrt(c))/2; 
            Console.WriteLine("bounds made!: {0}", r); 
            
            xmin = -r; 
            ymin = -r;
            xmax =  r; 
            ymax =  r; 
            return r; 
        }

        public void correctCoordinates(){
            if(screenWidth ==screenHeight){Console.WriteLine("did not have to correct\n"); return;}
            double shortestSide = screenHeight>screenWidth?screenWidth:screenHeight;
            double longestSide = screenHeight>screenWidth?screenHeight:screenWidth;

            double pixel_ratio = screenHeight/screenWidth;
            double plane_ratio = (ymax + Math.Abs(ymin))/(xmax+Math.Abs(xmin));

            double pixelToPlane_ratio = (ymax + Math.Abs(ymin))/shortestSide;
            double amountToAdd = (longestSide-shortestSide) * pixelToPlane_ratio;
            //Console.WriteLine("pixeltoplaneratio: {0} :: Amounttoadd: {1} :: pixelratio: {2} :: Screenwidth/height: {3} / {4}", pixelToPlane_ratio, amountToAdd, pixel_ratio, screenWidth, screenHeight);

            if(pixel_ratio > 1){
                ymax += amountToAdd/2;
                ymin -= amountToAdd/2;
            }
            if(pixel_ratio < 1){
                xmax += amountToAdd/2;
                xmin -= amountToAdd/2;
            }
            //Console.WriteLine("did correct: {0} {1} {2} {3} \n", ymax, ymin, xmax, xmin);
        } 

        double[] xy_conv(double x, double y){
            double[] result = new double[2];
            result[0] =  ((xmax-xmin)/(screenWidth)*x)+xmin;
            result[1] =  ((ymax-ymin)/(screenHeight)*y)+ymin;
            return result;
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
            Console.WriteLine("hello, let's see if we are dealing w a square {0} {1} {2} {3}", this.xmax, this.xmin, this.ymax, this.ymin); 
            Console.WriteLine("width and height: {0} {1}", width, height);
            Color pixCol = new Color(); 
            double[] data = new double[3];

            for(int y= 0; y < height; y++){
                for(int x = 0; x < width; x++){
                    int bail = 600;
                    double[] coor = xy_conv(x, y);
                    double[] ab = isInJuliaSet(coor[0], coor[1], bail);
                    //if(ab[0] + ab[1] <= r){
                        data[0] = 255-Math.Round(255*(ab[2]/bail));
                        data[1] = Math.Round(255*(ab[2]/bail));
                        data[2] = 255-Math.Round(255*(ab[2]/bail));
                    //}
                    pixCol = Color.FromArgb(
                        Convert.ToByte(data[0]),
                        Convert.ToByte(data[1]),
                        Convert.ToByte(data[2]),
                        255 
                    );
                    btmp.SetPixel(x,y,pixCol);
                }
            }
        }
    }

    class ImageRender
    {
        //class initializer
        public ImageRender(){ }
        //class initializer pt.2
        public ImageRender(int width, int height){
            screenWidth    = width; 
            screenHeight   = height;
        }
        //class properties
        public int screenWidth { get; set; }
        public int screenHeight { get; set; }

        public void drawFractal(){
            /*
            MandelFractal mandelFractal = new MandelFractal(screenWidth, screenHeight); 
            Bitmap bitmap1 = new Bitmap(screenWidth, screenHeight);
            mandelFractal.drawMandel(screenWidth, screenHeight, bitmap1);
            bitmap1.Save(@"mandelBackground.png");
            */
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

