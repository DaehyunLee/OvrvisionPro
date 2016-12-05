using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace ovrvision_calibration
{
	public partial class MFrom : Form
	{
		//Ovrvision Class
		COvrvision Ovrvision;
		//Thread
		Thread UpdateThread = null;
        bool ThreadEnd = false;

        //SettingForm
        SettingForm settingForm;

        int CalibrationImageNumSteps = 100;
        int CalibrationImageNum;

		public MFrom()
		{
            CalibrationImageNum = CalibrationImageNumSteps;
            InitializeComponent();
            this.KeyPreview = true;

			//Create Ovrvision Class
			Ovrvision = new COvrvision();
            settingForm = new SettingForm(Ovrvision);
		}

		private void runbutton_Click(object sender, EventArgs e)
		{
			if (runbutton.Text == "Open Ovrvision")
			{
                Ovrvision.useProcessingQuality = COvrvision.OV_CAMQT_DMS; 
                if (Ovrvision.Open(COvrvision.OV_CAMHD_FULL))
				{	//true
                    statelabel.Text = "State : Opened";
					runbutton.Text = "Close Ovrvision";

                    textBox1.Text = "Ovrvision was opened.\r\n";

					cameraPicLeft.Image = Ovrvision.imageDataLeft;	//(BGR)
					cameraPicRight.Image = Ovrvision.imageDataRight;

                    cabliButton.Enabled = true;
                    buttonSetting.Enabled = true;
				}
				else
				{	//false
					statelabel.Text = "State : Open Error.";
				}

				//Thread start
				ThreadEnd = false;
				UpdateThread = new Thread(new ThreadStart(MForm_UpdateThread));
				UpdateThread.Start();
			}
			else
			{
                ThreadEnd = true;
                UpdateThread.Join();
				if (Ovrvision.Close())
				{
                    statelabel.Text = "State : Closed";
					runbutton.Text = "Open Ovrvision";
                    cabliButton.Text = "Start Calibration";
					cameraPicRight.Image = null;
					cameraPicLeft.Image = null;

                    cabliButton.Enabled = false;
                    buttonSetting.Enabled = false;
				}
			}
		}

		private void MFrom_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (runbutton.Text != "Open Ovrvision")
			{
				//Close
                ThreadEnd = true;
                UpdateThread.Join();
				if (Ovrvision.Close())
				{
					statelabel.Text = "Closed";
					runbutton.Text = "Open Ovrvision";
                    cabliButton.Text = "Start Calibration";
					cameraPicRight.Image = null;
					cameraPicLeft.Image = null;

                    cabliButton.Enabled = false;
                    buttonSetting.Enabled = false;
				}
			}

            Ovrvision.Release();
		}

		private volatile bool UPDATE_LEFTDATA = false;
        private volatile bool UPDATE_RIGHTDATA = false;

        //Update Thread
        private void MForm_UpdateThread()
        {
            while (!ThreadEnd)
            {
                Ovrvision.UpdateCamera();

				UPDATE_LEFTDATA = true;
				UPDATE_RIGHTDATA = true;

                cameraPicLeft.Invalidate();
                cameraPicRight.Invalidate();
                
                while (UPDATE_LEFTDATA || UPDATE_RIGHTDATA)
                    Thread.Sleep(2);
            }
        }

        private void cameraPicLeft_Paint(object sender, PaintEventArgs e)
        {
            Ovrvision.UpdateLeft();
            UPDATE_LEFTDATA = false;

        }

        private void cameraPicRight_Paint(object sender, PaintEventArgs e)
        {
            Ovrvision.UpdateRight();
            UPDATE_RIGHTDATA = false;
        }


		//Calibration
		private void cabliButton_Click(object sender, EventArgs e)
		{
			if (cabliButton.Text == "Start Calibration")
			{
                ChessSizeForm csf = new ChessSizeForm();

                if(csf.ShowDialog(this) == DialogResult.OK)
                {
                    float tilesize = csf.GetTileSize();
                    if (tilesize > 5)
                    {
                        Ovrvision.InitializeCalibration(9, 6, (float)tilesize);
                        cabliButton.Text = "Find Chessboard";
                        textBox1.AppendText(String.Format("Start calibration.... tile size is {0}mm\r\n", tilesize));

						HowToUse htudialog = new HowToUse();
						htudialog.ShowDialog(this);
                    }

                }
			}
			else if (cabliButton.Text == "Find Chessboard")
			{
                cameraPicRight.Visible = false;
                cameraPicLeft.Visible = false;
                Thread.Sleep(25);

                int ret = Ovrvision.CalibFindChess();
                if (ret != 0)
                {
                    textBox1.AppendText(String.Format("[Success]Chess board was found: No.{0} / {1}\r\n", Ovrvision.CalibGetImageCount(), CalibrationImageNum));
                    textBox1.AppendText(String.Format("Left : {0}, Right : {1}\r\n", (ret&0x01)==0x01 ? "found" : "not found", (ret & 0x10) == 0x10 ? "found" : "not found"));
                    textBox1.AppendText(String.Format("Count Left : {0}, Right : {1}\r\n", Ovrvision.CalibGetImageCountIsolatedLeft(), Ovrvision.CalibGetImageCountIsolatedRight()));

                }
                else
                {
                    textBox1.AppendText(String.Format("[Failure]Can not find the chess board.\r\n"));
                }

                if (Ovrvision.CalibGetImageCount() >= CalibrationImageNum)
				{
					cabliButton.Text = "Create Parameters";
				}

                cameraPicRight.Visible = true;
                cameraPicLeft.Visible = true;
			}
			else if (cabliButton.Text == "Create Parameters")
			{
                textBox1.AppendText(String.Format("Setup in the data..... "));
				double result = Ovrvision.CalibSolveStereoParameter();
				textBox1.AppendText(String.Format("OK!\r\n"));
                Thread.Sleep(500); //0.5s wait
                textBox1.AppendText(String.Format("The calibration params was saved successfully.\r\n"));
                textBox1.AppendText(String.Format("LeftErr : {0}, RightErr : {1}\r\n", Ovrvision.GetErrLeft(), Ovrvision.GetErrRight()));
                textBox1.AppendText(String.Format("calibration error{0}\r\n", result));

                const double AcceptableErr = 0.1;
                if (result>0 && result < AcceptableErr)
                {
                    //Close
                    ThreadEnd = true;
                    UpdateThread.Join();
                    if (Ovrvision.Close())
                    {
                        statelabel.Text = "Closed";
                        runbutton.Text = "Open Ovrvision";
                        cabliButton.Text = "Start Calibration";
                        cameraPicRight.Image = null;
                        cameraPicLeft.Image = null;

                        cabliButton.Enabled = false;
                    }
                }
                else
                {
                    CalibrationImageNum += CalibrationImageNumSteps;
                    cabliButton.Text = "Find Chessboard";
                }

                textBox1.AppendText(String.Format("Ovrvision calibration is done.\r\n"));
			}
		}

        private void MFrom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C)
            {
                if(cabliButton.Enabled)
                 cabliButton_Click(sender, e);
            }
        }

		private void MFrom_Load(object sender, EventArgs e)
		{

		}

        private void buttonSetting_Click(object sender, EventArgs e)
        {
            if (!settingForm.Visible)
            {
                settingForm.ApplyItem();
                settingForm.Show();
            }
        }

        private void ComputeCalibration_Click(object sender, EventArgs e)
        {
            textBox1.AppendText(String.Format("Setup in the data..... "));
            double result = Ovrvision.CalibSolveStereoParameter();
            textBox1.AppendText(String.Format("OK!\r\n"));
            Thread.Sleep(500); //0.5s wait
            textBox1.AppendText(String.Format("The calibration params was saved successfully.\r\n"));
            textBox1.AppendText(String.Format("calibration error{0}\r\n", result));
            textBox1.AppendText(String.Format("LeftErr : {0}, RightErr : {1}\r\n", Ovrvision.GetErrLeft(), Ovrvision.GetErrRight() ));

            textBox1.AppendText(String.Format("Ovrvision calibration is done.\r\n"));
        }
    }
}
