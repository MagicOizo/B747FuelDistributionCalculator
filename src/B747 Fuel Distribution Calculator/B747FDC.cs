using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace B747_Fuel_Distribution_Calculator
{
    public partial class B747FDC : Form
    {
        double targetLoad;
        Aircraft[] aircrafts;
        long[] MainTank;
        long CenterTank;
        long[] ReserveTank;
        long StabelizerTank;
        bool calculated;

        const double FSECONOMY_GALON_PER_KG = 0.3794; //Get kg do: value / this; get gal do: value * this 
        const double KG_PER_LBS = 0.45359237; //Get kg do: value * this; get lbs do: value / this 

        public B747FDC()
        {
            InitializeComponent();
            targetLoad = 0;
            calculated = false;
            btnSetToXP.Enabled = false;
            aircrafts = new Aircraft[]
            {
                new Aircraft("SSG B747-8I", 60000,162643,438181,45923,520459,99500,1913453,new int[] {1,2,7,3,4,5,6,8}, new string[] {"Main 1", "Main 2", "Reserve 1", "Center", "Stabelizer", "Main 3", "Main 4", "Reserve 4" }),
                new Aircraft("SSG B747-8F", 60000, 162486, 436908, 45135, 515317, -1, 1804375, new int[] {1,2,6,3,-1,4,5,7}, new string[] {"Main 1", "Main 2", "Reserve 1", "Center", "", "Main 3", "Main 4", "Reserve 4" }),
                new Aircraft("mSparks B747-400", 60000, 136220, 381320, 40180, 521670, 100300, 1737410,new int[] {2,3,6,1,8,4,5,7}, new string[] {"Main 1", "Main 2", "Reserve 2", "Center", "Stabelizer", "Main 3", "Main 4", "Reserve 3" }),
                new Aircraft("Laminar B747-400", 60000, 135158, 381216, 39854, 521220, 100769, 1734447,new int[] {2,3,6,1,8,4,5,7}, new string[] {"Main 1", "Main 2", "Reserve 2", "Center", "Stabelizer", "Main 3", "Main 4", "Reserve 3" })
            };
            for(int i = 0; i < aircrafts.Length; i++)
            {
                cmbAircraft.Items.Add(aircrafts[i].AircraftName + " (max. " + ((double)aircrafts[i].CapacityLimit / 10000).ToString("#,##0.0000") + " ton)");
            }
            cmbAircraft.SelectedIndex = 0;
            radMetricFormat.Checked = true;
            lblTotal.Text = "";
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                calculateLoad();
                e.Handled = true;
                return;
            }
        }

        private void btnClac_Click(object sender, EventArgs e)
        {
            calculateLoad();
        }

        private void calculateLoad()
        {
            targetLoad = 0;
            if (!double.TryParse(txtInput.Text, out targetLoad))
            {
                MessageBox.Show("Input value not valid!", "Calculate Fuel Load", MessageBoxButtons.OK);
                return;
            }
            if(radImperialFormat.Checked)
            {
                targetLoad = Math.Min(aircrafts[cmbAircraft.SelectedIndex].CapacityLimit, targetLoad * KG_PER_LBS);
                targetLoad /= 1000;
            }
            else if (radGalonsFormat.Checked)
            {
                targetLoad = Math.Min(aircrafts[cmbAircraft.SelectedIndex].CapacityLimit, targetLoad / FSECONOMY_GALON_PER_KG);
                targetLoad /= 1000;
            }
            long targetLoadL = (long)(targetLoad * 10000);
            if(targetLoadL > aircrafts[cmbAircraft.SelectedIndex].CapacityLimit)
            {
                MessageBox.Show("Max Fuel Capacity exceeded!", "Calculate Fuel Load", MessageBoxButtons.OK);
                return;
            }
            MainTank = new long[] { 0, 0, 0, 0 };
            CenterTank = 0;
            ReserveTank = new long[] { 0, 0 };
            StabelizerTank = 0;

            fillTanks(targetLoadL);
            displayFormatedValues();
            calculated = true;
            btnSetToXP.Enabled = true;
        }

        private void fillTanks(long targetLoad)
        {

            //Step 1 up to 6.000 kg per Main Tank
            if (targetLoad < 4 * aircrafts[cmbAircraft.SelectedIndex].MainTreshold14)
            {
                MainTank[0] = (long)(targetLoad / 4);
                MainTank[1] = MainTank[0];
                MainTank[2] = MainTank[0];
                MainTank[3] = MainTank[0];
                return;
            }
            MainTank[0] = aircrafts[cmbAircraft.SelectedIndex].MainTreshold14;
            MainTank[1] = MainTank[0];
            MainTank[2] = MainTank[0];
            MainTank[3] = MainTank[0];
            long tempLoadSum = 4 * aircrafts[cmbAircraft.SelectedIndex].MainTreshold14;
            //Step 2 up to full Reserve Tanks 1 and 4
            if (targetLoad < tempLoadSum + (4 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14))
            {
                ReserveTank[0] = (long)((targetLoad - tempLoadSum) / 4);
                MainTank[1] += ReserveTank[0];
                MainTank[2] = MainTank[1];
                ReserveTank[1] = ReserveTank[0];
                return;
            }
            ReserveTank[0] = aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14;
            MainTank[1] += ReserveTank[0];
            MainTank[2] = MainTank[1];
            ReserveTank[1] = ReserveTank[0];
            tempLoadSum += 4 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14;
            //Step 3 up to full Main Tanks 1 and 4
            if (targetLoad < 4 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14 + 4 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14)
            {
                MainTank[0] += (long)((targetLoad - tempLoadSum) / 4);
                MainTank[1] += (long)((targetLoad - tempLoadSum) / 4);
                MainTank[2] = MainTank[1];
                MainTank[3] = MainTank[0];
                return;
            }
            MainTank[0] = aircrafts[cmbAircraft.SelectedIndex].MainLimit14;
            MainTank[1] = aircrafts[cmbAircraft.SelectedIndex].MainLimit14 + aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14;
            MainTank[2] = MainTank[1];
            MainTank[3] = MainTank[0];
            tempLoadSum = 4 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14 + 4 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14;
            //Step 4 up to full Main Tanks 2 and 3
            if (targetLoad < 2* aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14 + 2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14 + 2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit23)
            {
                MainTank[1] += (long)((targetLoad - tempLoadSum) / 2);
                MainTank[2] = MainTank[1];
                return;
            }
            MainTank[1] = aircrafts[cmbAircraft.SelectedIndex].MainLimit23;
            MainTank[2] = MainTank[1];
            tempLoadSum = 2 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14 + 2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14 + 2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit23;
            //Step 5 up to full Center Tank
            if (targetLoad < tempLoadSum + aircrafts[cmbAircraft.SelectedIndex].CenterLimit)
            {
                CenterTank = (long)(targetLoad - tempLoadSum);
                return;
            }
            CenterTank = aircrafts[cmbAircraft.SelectedIndex].CenterLimit;
            tempLoadSum += aircrafts[cmbAircraft.SelectedIndex].CenterLimit;
            //Step 6 up to full Stabelizer Tank
            StabelizerTank = (long)(targetLoad - tempLoadSum);
        }

        private long sumTank()
        {
            return MainTank[0] + MainTank[1] + MainTank[2] + MainTank[3] + ReserveTank[0] + ReserveTank[1] + StabelizerTank + CenterTank;
        }

        private void displayFormatedValues()
        {
            if(MainTank == null || ReserveTank == null || MainTank.Length < 4 || ReserveTank.Length < 2)
            {
                return;
            }
            //kg Values will always get displayed as kg
            textBox9.Text = ((float)MainTank[0] / 10).ToString("#,##0.0");
            textBox10.Text = ((float)MainTank[1] / 10).ToString("#,##0.0");
            textBox11.Text = ((float)ReserveTank[0] / 10).ToString("#,##0.0");
            textBox12.Text = ((float)CenterTank / 10).ToString("#,##0.0");
            textBox13.Text = ((float)StabelizerTank / 10).ToString("#,##0.0");
            textBox14.Text = ((float)MainTank[2] / 10).ToString("#,##0.0");
            textBox15.Text = ((float)MainTank[3] / 10).ToString("#,##0.0");
            textBox16.Text = ((float)ReserveTank[1] / 10).ToString("#,##0.0");
            //other values will be displayed on basis of selected format
            if (radMetricFormat.Checked)
            {
                textBox1.Text = Math.Round(((double)MainTank[0] / 10000), 1).ToString("#,##0.0");
                textBox2.Text = Math.Round(((double)MainTank[1] / 10000), 1).ToString("#,##0.0");
                textBox3.Text = Math.Round(((double)ReserveTank[0] / 10000), 1).ToString("#,##0.0");
                textBox4.Text = Math.Round(((double)CenterTank / 10000), 1).ToString("#,##0.0");
                textBox5.Text = Math.Round(((double)StabelizerTank / 10000), 1).ToString("#,##0.0");
                textBox6.Text = Math.Round(((double)MainTank[2] / 10000), 1).ToString("#,##0.0");
                textBox7.Text = Math.Round(((double)MainTank[3] / 10000), 1).ToString("#,##0.0");
                textBox8.Text = Math.Round(((double)ReserveTank[1] / 10000), 1).ToString("#,##0.0");
                lblTotal.Text = "Total: " + ((float)sumTank() / 10).ToString("#,##0.0") + " kg or " + (Math.Round(((double)MainTank[0] / 10000), 1) + Math.Round(((double)MainTank[1] / 10000), 1) + Math.Round(((double)MainTank[2] / 10000), 1) + Math.Round(((double)MainTank[3] / 10000), 1) + Math.Round(((double)ReserveTank[0] / 10000), 1) + Math.Round(((double)ReserveTank[1] / 10000), 1) + Math.Round(((double)CenterTank / 10000), 1) + Math.Round(((double)StabelizerTank / 10000), 1)).ToString("#,##0.0") + " ton";
                return;
            }
            if (radImperialFormat.Checked)
            {
                textBox1.Text = Math.Round(((double)MainTank[0] / 10) / KG_PER_LBS, 1).ToString("#,##0.0");
                textBox2.Text = Math.Round(((double)MainTank[1] / 10) / KG_PER_LBS, 1).ToString("#,##0.0");
                textBox3.Text = Math.Round(((double)ReserveTank[0] / 10) / KG_PER_LBS, 1).ToString("#,##0.0");
                textBox4.Text = Math.Round(((double)CenterTank / 10) / KG_PER_LBS, 1).ToString("#,##0.0");
                textBox5.Text = Math.Round(((double)StabelizerTank / 10) / KG_PER_LBS, 1).ToString("#,##0.0");
                textBox6.Text = Math.Round(((double)MainTank[2] / 10) / KG_PER_LBS, 1).ToString("#,##0.0");
                textBox7.Text = Math.Round(((double)MainTank[3] / 10) / KG_PER_LBS, 1).ToString("#,##0.0");
                textBox8.Text = Math.Round(((double)ReserveTank[1] / 10) / KG_PER_LBS, 1).ToString("#,##0.0");
                lblTotal.Text = "Total: " + Math.Round(((double)sumTank() / 10) / KG_PER_LBS, 1).ToString("#,##0.0") + " lbs";
                return;
            }
            if (radGalonsFormat.Checked)
            {
                textBox1.Text = Math.Round(((double)MainTank[0] / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                textBox2.Text = Math.Round(((double)MainTank[1] / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                textBox3.Text = Math.Round(((double)ReserveTank[0] / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                textBox4.Text = Math.Round(((double)CenterTank / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                textBox5.Text = Math.Round(((double)StabelizerTank / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                textBox6.Text = Math.Round(((double)MainTank[2] / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                textBox7.Text = Math.Round(((double)MainTank[3] / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                textBox8.Text = Math.Round(((double)ReserveTank[1] / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                lblTotal.Text = "Total: " + Math.Round(((double)sumTank() / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0") + " gal of JetA";
                return;
            }
        }

        private void txtInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(!char.IsControl(e.KeyChar) && !char.IsNumber(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }

        private void btnGetFromXP_Click(object sender, EventArgs e)
        {
            try
            {
                using (XPlaneConnect xpc = new XPlaneConnect(100))
                {
                    // Ensure connection established.
                    float[] tanks =  xpc.getDREF("sim/flightmodel/weight/m_fuel"); // float[9] in kgs
                    if (tanks.Length >= 8)
                    {
                        MainTank = new long[4];
                        ReserveTank = new long[2];
                        switch (cmbAircraft.SelectedIndex) 
                        {
                            case 0: //SSG B747-8I
                            case 1: //SSG B747-8F
                                MainTank[0] = (int)(tanks[0] * 10);
                                MainTank[1] = (int)(tanks[1] * 10);
                                CenterTank = (int)(tanks[2] * 10);
                                StabelizerTank = (int)(tanks[3] * 10);
                                MainTank[2] = (int)(tanks[4] * 10);
                                MainTank[3] = (int)(tanks[5] * 10);
                                ReserveTank[0] = (int)(tanks[6] * 10);
                                ReserveTank[1] = (int)(tanks[7] * 10);
                                break;
                            default: //mSparks 747-400 and Laminar B747-400
                                MainTank[0] = (int)(tanks[1] * 10);
                                MainTank[1] = (int)(tanks[2] * 10);
                                CenterTank = (int)(tanks[0] * 10);
                                StabelizerTank = (int)(tanks[7] * 10);
                                MainTank[2] = (int)(tanks[3] * 10);
                                MainTank[3] = (int)(tanks[4] * 10);
                                ReserveTank[0] = (int)(tanks[5] * 10);
                                ReserveTank[1] = (int)(tanks[6] * 10);
                                break;
                        }
                        displayFormatedValues();
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Unable to set up the connection. (Error message was '" + ex.Message + "'.)");
                Console.WriteLine(ex.StackTrace.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong with one of the commands. (Error message was '" + ex.Message + "'.)");
                Console.WriteLine(ex.StackTrace.ToString());
            }
        }

        private void btnSetToXP_Click(object sender, EventArgs e)
        {
            if (!calculated)
            {
                return;
            }
            try
            {
                using (XPlaneConnect xpc = new XPlaneConnect(100))
                {
                    // Ensure connection established.
                    float[] tanks;
                    switch(cmbAircraft.SelectedIndex)
                    {
                        case 0: //SSG B747-8I
                            tanks = new float[] { (float)MainTank[0] / 10, (float)MainTank[1] / 10, (float)CenterTank / 10, (float)StabelizerTank / 10, (float)MainTank[2] / 10, (float)MainTank[3] / 10, (float)ReserveTank[0] / 10, (float)ReserveTank[1] / 10, 0F };
                            break;
                        case 1: //SSG B747-8F
                            tanks = new float[] { (float)MainTank[0] / 10, (float)MainTank[1] / 10, (float)CenterTank / 10, 0F, (float)MainTank[2] / 10, (float)MainTank[3] / 10, (float)ReserveTank[0] / 10, (float)ReserveTank[1] / 10, 0F };
                            break;
                        default: //mSparks 747-400 and Laminar B747-400
                            tanks = new float[] { (float)CenterTank / 10, (float)MainTank[0] / 10, (float)MainTank[1] / 10, (float)MainTank[2] / 10, (float)MainTank[3] / 10, (float)ReserveTank[0] / 10, (float)ReserveTank[1] / 10, (float)StabelizerTank / 10,  0F };
                            break;
                    }

                    xpc.sendDREF("sim/flightmodel/weight/m_fuel",tanks); // float[9] in kgs
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Unable to set up the connection. (Error message was '" + ex.Message + "'.)");
                Console.WriteLine(ex.StackTrace.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong with one of the commands. (Error message was '" + ex.Message + "'.)");
                Console.WriteLine(ex.StackTrace.ToString());
            }
        }

        private void cmbAircraft_SelectedIndexChanged(object sender, EventArgs e)
        {
            label2.Text = "XP Tank " + aircrafts[cmbAircraft.SelectedIndex].Labels[0];
            label3.Text = "XP Tank " + aircrafts[cmbAircraft.SelectedIndex].Labels[1];
            label1.Text = "XP Tank " + aircrafts[cmbAircraft.SelectedIndex].Labels[2];
            label4.Text = "XP Tank " + aircrafts[cmbAircraft.SelectedIndex].Labels[3];
            label5.Text = "XP Tank " + aircrafts[cmbAircraft.SelectedIndex].Labels[4];
            if (aircrafts[cmbAircraft.SelectedIndex].Labels[4] == -1)
            {
                label5.Text = "";
            }
            label6.Text = "XP Tank " + aircrafts[cmbAircraft.SelectedIndex].Labels[5];
            label15.Text = "XP Tank " + aircrafts[cmbAircraft.SelectedIndex].Labels[6];
            label7.Text = "XP Tank " + aircrafts[cmbAircraft.SelectedIndex].Labels[7];
            label8.Text = aircrafts[cmbAircraft.SelectedIndex].TankNames[0];
            label9.Text = aircrafts[cmbAircraft.SelectedIndex].TankNames[1];
            label10.Text = aircrafts[cmbAircraft.SelectedIndex].TankNames[2];
            label11.Text = aircrafts[cmbAircraft.SelectedIndex].TankNames[3];
            label12.Text = aircrafts[cmbAircraft.SelectedIndex].TankNames[4];
            label13.Text = aircrafts[cmbAircraft.SelectedIndex].TankNames[5];
            label16.Text = aircrafts[cmbAircraft.SelectedIndex].TankNames[6];
            label14.Text = aircrafts[cmbAircraft.SelectedIndex].TankNames[7];
        }

        private void chkTopMost_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = chkTopMost.Checked;
        }

        private void radFormats_CheckedChanged(object sender, EventArgs e)
        {
            if(radMetricFormat.Checked)
            {
                lblNumberFormat.Text = "ton";
                lblInputFormat.Text = "x 1000 kg (ton)";
                for(int i = 0; i < aircrafts.Length; i++)
                {
                    cmbAircraft.Items[i] = aircrafts[i].AircraftName + " (max. " + ((double)aircrafts[i].CapacityLimit / 10000).ToString("#,##0.0000") + " ton)";
            }
                txtInput.Text = "0";
                if(calculated)
                {
                    txtInput.Text = targetLoad.ToString("#,##0.0");
                }
                displayFormatedValues();
                return;
            }
            if (radImperialFormat.Checked)
            {
                lblNumberFormat.Text = "lbs";
                lblInputFormat.Text = "lbs";
                for (int i = 0; i < aircrafts.Length; i++)
                {
                    cmbAircraft.Items[i] = aircrafts[i].AircraftName + " (max. " + ((double)aircrafts[i].CapacityLimit / 10 / KG_PER_LBS).ToString("#,##0.0") + " lbs)";
                }
                if (calculated)
                {
                    txtInput.Text = Math.Round((double)targetLoad * 1000/KG_PER_LBS,1).ToString("#,##0.0");
                }
                displayFormatedValues();
                return;
            }
            if (radGalonsFormat.Checked)
            {
                lblNumberFormat.Text = "gal";
                lblInputFormat.Text = "gal of JetA";
                for (int i = 0; i < aircrafts.Length; i++)
                {
                    cmbAircraft.Items[i] = aircrafts[i].AircraftName + " (max. " + ((double)aircrafts[i].CapacityLimit / 10 *FSECONOMY_GALON_PER_KG).ToString("#,##0.0") + " gal of JetA)";
                }
                if (calculated)
                {
                    txtInput.Text = Math.Round((double)targetLoad * 1000 * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                }
                displayFormatedValues();
                return;
            }
        }
    }
}
