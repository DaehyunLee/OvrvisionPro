// OpenCV.cpp : コンソール アプリケーションのエントリ ポイントを定義します。
//

#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/imgproc.hpp>

#include "ovrvision_pro.h"

#define CROP_W 800
#define CROP_H 600

using namespace cv;
using namespace OVR;

static 	OvrvisionPro ovrvision;

int callback(void *pItem, const char *extensions)
{
	if (extensions != NULL)
	{
		puts(extensions);
	}
	return 0;
}

int main(int argc, char* argv[])
{
	if (ovrvision.Open(0, Camprop::OV_CAMHD_FULL))
	{
		int ksize = 5;
		int width = ovrvision.GetCamWidth();
		int height = ovrvision.GetCamHeight();
		ROI roi = {(width - CROP_W) / 2, (height - CROP_H) / 2, CROP_W, CROP_H};
		Mat left(roi.height, roi.width, CV_8UC4);
		Mat right(roi.height, roi.width, CV_8UC4);
		Mat lBlur(roi.height, roi.width, CV_8UC4);	// work
		Mat rBlur(roi.height, roi.width, CV_8UC4);
		Mat hsv(roi.height, roi.width, CV_8UC3);
		Mat YCrCb(roi.height, roi.width, CV_8UC3);
		Mat result(roi.height, roi.width, CV_8UC3);
		Mat YRB[3];
		Mat HSV[3];
		Mat histgram(256, 256, CV_16UC1);

		//Sync
		ovrvision.SetCameraSyncMode(true);

		Camqt mode = Camqt::OV_CAMQT_DMSRMP;
		bool show = true;

		for (bool loop = true; loop;)
		{
			//DWORD begin = GetTickCount();
			if (show)
			{
				// Capture frame
				ovrvision.Capture(mode);

				// Retrieve frame data
				//ovrvision.GetCamImageBGRA(left.data, Cameye::OV_CAMEYE_LEFT);
				//ovrvision.GetCamImageBGRA(right.data, Cameye::OV_CAMEYE_RIGHT);
				ovrvision.GetStereoImageBGRA(left.data, right.data, roi);

				// ここでOpenCVでの加工など
				if (0 < ksize)
				{
					//medianBlur(left, lBlur, ksize);
					//medianBlur(right, rBlur, ksize);
					cvtColor(right, YCrCb, CV_BGR2YCrCb);
					cvtColor(left, hsv, CV_BGR2HSV_FULL);
					split(hsv, HSV);
					//split(YCrCb, YRB);

					histgram.setTo(Scalar::all(0));
					result.setTo(Scalar::all(0));
					for (uint y = 0; y < roi.height; y++)
					{
						Point3_<uchar> *row = hsv.ptr<Point3_<uchar>>(y);
						//Vec4b *pixel = right.ptr<Vec4b>(y);
						Vec3b *pixel = result.ptr<Vec3b>(y);
						for (uint x = 0; x < roi.width; x++)
						{
							uchar h = row[x].x;
							uchar s = row[x].y;
							if (20 <= h && h <= 40 && 75 < s && s < 200)
							{
								pixel[x] = YCrCb.at<Vec3b>(y, x);
								pixel[x][0] = h;
								//pixel[x][3] = s;
							}
							ushort *hs = histgram.ptr<ushort>(s, h);
							hs[0]++;
						}
					}

					// Show frame data
					imshow("Left", left);
					imshow("Right", right);
					imshow("HSV", hsv);
					imshow("Hue", HSV[0]);
					imshow("Sat", HSV[1]);
					//imshow("YCrCb", YCrCb);
					imshow("Result", result);
					//imshow("Histgram", histgram);
					//imshow("Cr", YRB[1]);
					//imshow("Cb", YRB[2]);
				}
				else
				{
					// Show frame data
					imshow("Left", left);
					imshow("Right", right);
				}

			}
			else
			{
				ovrvision.Capture(mode);
			}

			//DWORD end = GetTickCount();
			//printf("%f fps %d ms/frame\n", 1000.0f / (end - begin), (end - begin));

			switch (waitKey(1))
			{
			case 'q':
				loop = false;
				break;

			case 'r':
				mode = Camqt::OV_CAMQT_DMSRMP;
				break;

			case 'd':
				mode = Camqt::OV_CAMQT_DMS;
				break;

			case 's':
				show = true;
				break;

			case 'n':
				show = false;
				break;

			case '0':
			case '1':
				ksize = 0;
				break;

			case '3':
				ksize = 3;
				break;

			case '5':
				ksize = 5;
				break;

			case '7':
				ksize = 7;
				break;

			case '9':
				ksize = 9;
				break;

			case ' ':
				//imwrite("histgram.png", histgram);
				imwrite("Hue.png", HSV[0]);
				imwrite("Sat.png", HSV[1]);
				imwrite("hsv.png", hsv);
				imwrite("HCrCb.png", result);
				imwrite("YCrCb.png", YCrCb);
				imwrite("left.png", left);
				imwrite("right.png", right);
				break;

			case 'e':
				if (callback != NULL)
				{
					callback(NULL, "Device Extensions");
				}
				ovrvision.OpenCLExtensions(callback, NULL);
				break;
			}
		}
	}
	else
	{
		puts("FAILED TO OPEN CAMERA");
	}
	return 0;
}


