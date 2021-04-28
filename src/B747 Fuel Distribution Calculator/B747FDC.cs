using System;
using System.Drawing;
using System.Net.Sockets;
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
        int indexSelection;

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
                new Aircraft("SSG B747-8I", 60000,162643,438181,45923,520459,99500,1913453,new int[] {1,2,7,3,4,5,6,8}, new string[] {"Main 1", "Main 2", "Reserve 1", "Center", "Stabelizer", "Main 3", "Main 4", "Reserve 4" },Properties.Resources.ScematicB748I),
                new Aircraft("SSG B747-8F", 60000, 162486, 436908, 45135, 515317, -1, 1804375, new int[] {1,2,6,3,-1,4,5,7}, new string[] {"Main 1", "Main 2", "Reserve 1", "Center", "", "Main 3", "Main 4", "Reserve 4" },Properties.Resources.ScematicB748F),
                new Aircraft("mSparks B747-400", 181437, 136220, 381320, 40180, 521670, 100300, 1737410,new int[] {2,3,6,1,8,4,5,7}, new string[] {"Main 1", "Main 2", "Reserve 2", "Center", "Stabelizer", "Main 3", "Main 4", "Reserve 3" },Properties.Resources.ScematicB744),
                new Aircraft("Laminar B747-400", 181437, 135158, 381216, 39854, 521220, 100769, 1734447,new int[] {2,3,6,1,8,4,5,7}, new string[] {"Main 1", "Main 2", "Reserve 2", "Center", "Stabelizer", "Main 3", "Main 4", "Reserve 3" },Properties.Resources.ScematicB744)
            };
            for(int i = 0; i < aircrafts.Length; i++)
            {
                cmbAircraft.Items.Add(aircrafts[i].AircraftName + " (max. " + ((double)aircrafts[i].CapacityLimit / 10000).ToString("#,##0.0000") + " ton)");
            }
            cmbAircraft.SelectedIndex = 0;
            indexSelection = 0;
            radMetricFormat.Checked = true;
            lblTotal.Text = "";
            this.Height = 223;
            picVisualization.Visible = false;
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
            resetCalculatiion();

            fillTanks(targetLoadL);
            displayFormatedValues();
            if (picVisualization.Visible == true)
            {
                picVisualization.Invalidate();
            }
            calculated = true;
            btnSetToXP.Enabled = true;
        }

        private void fillTanks(long targetLoad)
        {
            long tempLoadSum;
            switch (cmbAircraft.SelectedIndex)
            {
                case 0:
                case 1:
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
                    tempLoadSum = 4 * aircrafts[cmbAircraft.SelectedIndex].MainTreshold14;
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
                    if (targetLoad < 2 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14 + 2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14 + 2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit23)
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
                    break;
                default:
                    //Step 1 Main 1 - 4 full up to Main 1/4 Limit
                    if (targetLoad < 4 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14)
                    {
                        MainTank[0] = (long)(targetLoad / 4);
                        MainTank[1] = MainTank[0];
                        MainTank[2] = MainTank[0];
                        MainTank[3] = MainTank[0];
                        return;
                    }
                    MainTank[0] = aircrafts[cmbAircraft.SelectedIndex].MainLimit14;
                    MainTank[1] = MainTank[0];
                    MainTank[2] = MainTank[0];
                    MainTank[3] = MainTank[0];
                    tempLoadSum = 4 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14;
                    //Step 2 up to Main 2/3 Threshold
                    if (targetLoad < (2* aircrafts[cmbAircraft.SelectedIndex].MainLimit14) + (2 * aircrafts[cmbAircraft.SelectedIndex].MainTreshold14))
                    {
                        MainTank[1] += (long)((targetLoad - tempLoadSum) / 2);
                        MainTank[2] = MainTank[1];
                        return;
                    }
                    MainTank[1] = aircrafts[cmbAircraft.SelectedIndex].MainTreshold14;
                    MainTank[2] = MainTank[1];
                    tempLoadSum = (2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14) + (2 * aircrafts[cmbAircraft.SelectedIndex].MainTreshold14);
                    //Step 3 up to full Main Tanks 1 and 4
                    if (targetLoad < tempLoadSum + (2 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14))
                    {
                        ReserveTank[0] = (long)((targetLoad - tempLoadSum) / 2);
                        ReserveTank[1] = ReserveTank[0];
                        return;
                    }
                    ReserveTank[0] = aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14;
                    ReserveTank[1] = ReserveTank[0];
                    tempLoadSum += (2 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14);
                    //Step 4 up to full Main Tanks 2 and 3
                    if (targetLoad < 2 * aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14 + 2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit14 + 2 * aircrafts[cmbAircraft.SelectedIndex].MainLimit23)
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
                    break;
            }
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
            textBox1.Text = getFormatedValue((double)MainTank[0], false);
            textBox2.Text = getFormatedValue((double)MainTank[1], false);
            textBox3.Text = getFormatedValue((double)ReserveTank[0], false);
            textBox4.Text = getFormatedValue((double)CenterTank, false);
            textBox5.Text = getFormatedValue((double)StabelizerTank, false);
            textBox6.Text = getFormatedValue((double)MainTank[2], false);
            textBox7.Text = getFormatedValue((double)MainTank[3], false);
            textBox8.Text = getFormatedValue((double)ReserveTank[1], false);
            if (radMetricFormat.Checked)
            {
                lblTotal.Text = "Total: " + ((float)sumTank() / 10).ToString("#,##0.0") + " kg or " + (Math.Round(((double)MainTank[0] / 10000), 1) + Math.Round(((double)MainTank[1] / 10000), 1) + Math.Round(((double)MainTank[2] / 10000), 1) + Math.Round(((double)MainTank[3] / 10000), 1) + Math.Round(((double)ReserveTank[0] / 10000), 1) + Math.Round(((double)ReserveTank[1] / 10000), 1) + Math.Round(((double)CenterTank / 10000), 1) + Math.Round(((double)StabelizerTank / 10000), 1)).ToString("#,##0.0") + " ton";
                return;
            }
            if (radImperialFormat.Checked)
            {
                lblTotal.Text = "Total: " + Math.Round(((double)sumTank() / 10) / KG_PER_LBS, 1).ToString("#,##0.0") + " lbs";
                return;
            }
            if (radGalonsFormat.Checked)
            {
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
            if (cmbAircraft.SelectedIndex != indexSelection)
            {
                resetCalculatiion();
            }
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
            if(picVisualization.Visible == true)
            {
                picVisualization.BackgroundImage = aircrafts[cmbAircraft.SelectedIndex].Visualization;
                picVisualization.Invalidate();
            }
            indexSelection = cmbAircraft.SelectedIndex;
        }

        private void resetCalculatiion()
        {
            MainTank = new long[] { 0, 0, 0, 0 };
            CenterTank = 0;
            ReserveTank = new long[] { 0, 0 };
            StabelizerTank = 0;
            calculated = false;
            btnSetToXP.Enabled = false;
            displayFormatedValues();
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
                    cmbAircraft.Items[i] = aircrafts[i].AircraftName + " (max. " + getFormatedValue((double)aircrafts[i].CapacityLimit,true) + ")";
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
                    cmbAircraft.Items[i] = aircrafts[i].AircraftName + " (max. " + getFormatedValue((double)aircrafts[i].CapacityLimit, true) + ")";
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
                    cmbAircraft.Items[i] = aircrafts[i].AircraftName + " (max. " + getFormatedValue((double)aircrafts[i].CapacityLimit, true) + " of JetA)";
                }
                if (calculated)
                {
                    txtInput.Text = Math.Round((double)targetLoad * 1000 * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0");
                }
                displayFormatedValues();
                return;
            }
        }

        private void btnVisual_Click(object sender, EventArgs e)
        {
            if(btnVisual.Text== "↓ Expand visualazation ↓")
            {
                btnVisual.Text = "↑ Contract visualazation ↑";
                this.Height = 596;
                picVisualization.Visible = true;
                picVisualization.Invalidate();
            }
            else
            {
                btnVisual.Text = "↓ Expand visualazation ↓";
                this.Height = 223;
                picVisualization.Visible = false;
            }
        }

        private void picVisualization_MouseClick(object sender, MouseEventArgs e)
        {
            Console.WriteLine(e.Location.ToString());
            picVisualization.Invalidate();
        }

        private string getFormatedValue(double kgs, bool withUnit)
        {
            string unitText = "";
            if (radMetricFormat.Checked)
            {
                if(withUnit)
                {
                    unitText = " ton";
                }
                return Math.Round((kgs / 10000), 1).ToString("#,##0.0") + unitText;
            }
            if (radImperialFormat.Checked)
            {
                if (withUnit)
                {
                    unitText = " lbs";
                }
                return Math.Round((kgs / 10) / KG_PER_LBS, 1).ToString("#,##0.0") + unitText;
            }
            if (withUnit)
            {
                unitText = " gal";
            }
            return Math.Round((kgs / 10) * FSECONOMY_GALON_PER_KG, 1).ToString("#,##0.0") + unitText;
        }

        private void picVisualization_Paint(object sender, PaintEventArgs e)
        {
            Pen tankBorder = new Pen(Brushes.Black, 2);
            Font prozentFont = new Font(FontFamily.GenericSansSerif, 12F, FontStyle.Bold);
            Font valueFont = new Font(FontFamily.GenericSansSerif, 8.25F, FontStyle.Regular);
            if (calculated)
            {
                //Reserve 0
                float tempPercent = (float)ReserveTank[0] / (float)aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14;
                string tempText = (tempPercent * 100F).ToString((tempPercent < 0.1) ?"0.0":"0") + "%";
                e.Graphics.FillRectangle(Brushes.DarkBlue, 164, (float)(350F - (50F * tempPercent)), 50, (50F * tempPercent)); 
                SizeF tempTextSize = e.Graphics.MeasureString(tempText, prozentFont);
                e.Graphics.DrawString(tempText, prozentFont, Brushes.White, 189 - (tempTextSize.Width / 2), 302);
                e.Graphics.DrawRectangle(tankBorder, 164, 300, 50, 50);
                tempText = getFormatedValue((double)ReserveTank[0], true);
                tempTextSize = e.Graphics.MeasureString(tempText, valueFont);
                e.Graphics.DrawString(tempText, valueFont, Brushes.Black, 189 - (tempTextSize.Width / 2), 352);

                //Reserve 1
                tempPercent = (float)ReserveTank[1] / (float)aircrafts[cmbAircraft.SelectedIndex].ReserveLimit14;
                tempText = (tempPercent * 100F).ToString((tempPercent < 0.1) ? "0.0" : "0") + "%";
                e.Graphics.FillRectangle(Brushes.DarkBlue, 743, (float)(350F - (50F * tempPercent)), 50, (50F * tempPercent)); 
                tempTextSize = e.Graphics.MeasureString(tempText, prozentFont);
                e.Graphics.DrawString(tempText, prozentFont, Brushes.White, 768 - (tempTextSize.Width / 2), 302);
                e.Graphics.DrawRectangle(tankBorder, 743, 300, 50, 50);
                tempText = getFormatedValue((double)ReserveTank[1], true);
                tempTextSize = e.Graphics.MeasureString(tempText, valueFont);
                e.Graphics.DrawString(tempText, valueFont, Brushes.Black, 768 - (tempTextSize.Width / 2), 352);

                //Main 0
                tempPercent = (float)MainTank[0] / (float)aircrafts[cmbAircraft.SelectedIndex].MainLimit14;
                tempText = (tempPercent * 100F).ToString((tempPercent < 0.1) ? "0.0" : "0") + "%";
                e.Graphics.FillRectangle(Brushes.DarkBlue, 262, (float)(300F - (90F * tempPercent)), 70, (90F * tempPercent));
                tempTextSize = e.Graphics.MeasureString(tempText, prozentFont);
                e.Graphics.DrawString(tempText, prozentFont, Brushes.White, 297 - (tempTextSize.Width / 2), 212);
                e.Graphics.DrawRectangle(tankBorder, 262, 210, 70, 90);
                tempText = getFormatedValue((double)MainTank[0], true);
                tempTextSize = e.Graphics.MeasureString(tempText, valueFont);
                e.Graphics.DrawString(tempText, valueFont, Brushes.Black, 297 - (tempTextSize.Width / 2), 302);

                //Main 1
                tempPercent = (float)MainTank[1] / (float)aircrafts[cmbAircraft.SelectedIndex].MainLimit23;
                tempText = (tempPercent * 100F).ToString((tempPercent < 0.1) ? "0.0" : "0") + "%";
                e.Graphics.FillRectangle(Brushes.DarkBlue, 361, (float)(250F - (130F * tempPercent)), 70, (130F * tempPercent));
                tempTextSize = e.Graphics.MeasureString(tempText, prozentFont);
                e.Graphics.DrawString(tempText, prozentFont, Brushes.White, 396 - (tempTextSize.Width / 2), 122);
                e.Graphics.DrawRectangle(tankBorder, 361, 120, 70, 130);
                tempText = getFormatedValue((double)MainTank[1], true);
                tempTextSize = e.Graphics.MeasureString(tempText, valueFont);
                e.Graphics.DrawString(tempText, valueFont, Brushes.Black, 396 - (tempTextSize.Width / 2), 252);

                //Main 2
                tempPercent = (float)MainTank[2] / (float)aircrafts[cmbAircraft.SelectedIndex].MainLimit23;
                tempText = (tempPercent * 100F).ToString((tempPercent < 0.1) ? "0.0" : "0") + "%";
                e.Graphics.FillRectangle(Brushes.DarkBlue, 525, (float)(250F - (130F * tempPercent)), 70, (130F * tempPercent));
                tempTextSize = e.Graphics.MeasureString(tempText, prozentFont);
                e.Graphics.DrawString(tempText, prozentFont, Brushes.White, 560 - (tempTextSize.Width / 2), 122);
                e.Graphics.DrawRectangle(tankBorder, 525, 120, 70, 130);
                tempText = getFormatedValue((double)MainTank[2], true);
                tempTextSize = e.Graphics.MeasureString(tempText, valueFont);
                e.Graphics.DrawString(tempText, valueFont, Brushes.Black, 560 - (tempTextSize.Width / 2), 252);

                //Main 3
                tempPercent = (float)MainTank[3] / (float)aircrafts[cmbAircraft.SelectedIndex].MainLimit14;
                tempText = (tempPercent * 100F).ToString((tempPercent < 0.1) ? "0.0" : "0") + "%";
                e.Graphics.FillRectangle(Brushes.DarkBlue, 625, (float)(300F - (90F * tempPercent)), 70, (90F * tempPercent));
                tempTextSize = e.Graphics.MeasureString(tempText, prozentFont);
                e.Graphics.DrawString(tempText, prozentFont, Brushes.White, 660 - (tempTextSize.Width / 2), 212);
                e.Graphics.DrawRectangle(tankBorder, 625, 210, 70, 90);
                tempText = getFormatedValue((double)MainTank[3], true);
                tempTextSize = e.Graphics.MeasureString(tempText, valueFont);
                e.Graphics.DrawString(tempText, valueFont, Brushes.Black, 660 - (tempTextSize.Width / 2), 302);

                //Center
                tempPercent = (float)CenterTank / (float)aircrafts[cmbAircraft.SelectedIndex].CenterLimit;
                tempText = (tempPercent * 100F).ToString((tempPercent < 0.1) ? "0.0" : "0") + "%";
                e.Graphics.FillRectangle(Brushes.DarkBlue, 452, (float)(236F - (170F * tempPercent)), 53, (170F * tempPercent));
                tempTextSize = e.Graphics.MeasureString(tempText, prozentFont);
                e.Graphics.DrawString(tempText, prozentFont, Brushes.White, 478.5F - (tempTextSize.Width / 2), 68);
                e.Graphics.DrawRectangle(tankBorder, 452, 66, 53, 170);
                tempText = getFormatedValue((double)CenterTank, true);
                tempTextSize = e.Graphics.MeasureString(tempText, valueFont);
                e.Graphics.DrawString(tempText, valueFont, Brushes.Black, 478.5F - (tempTextSize.Width / 2), 238);

                //Sabelizer
                if (aircrafts[cmbAircraft.SelectedIndex].StabLimit > 0)
                {
                    tempPercent = (float)StabelizerTank / (float)aircrafts[cmbAircraft.SelectedIndex].StabLimit;
                    tempText = (tempPercent * 100F).ToString((tempPercent < 0.1) ? "0.0" : "0") + "%";
                    e.Graphics.FillRectangle(Brushes.DarkBlue, 445, (float)(363F - (70F * tempPercent)), 65, (70F * tempPercent));
                    tempTextSize = e.Graphics.MeasureString(tempText, prozentFont);
                    e.Graphics.DrawString(tempText, prozentFont, Brushes.White, 477.5F - (tempTextSize.Width / 2), 295);
                    e.Graphics.DrawRectangle(tankBorder, 445, 293, 65, 70);
                    tempText = getFormatedValue((double)StabelizerTank, true);
                    tempTextSize = e.Graphics.MeasureString(tempText, valueFont);
                    e.Graphics.DrawString(tempText, valueFont, Brushes.Black, 477.5F - (tempTextSize.Width / 2), 365);
                }

                // TANK/ENG Message
                tempText = "Take Off with \"Tank to Engine\"";
                if(cmbAircraft.SelectedIndex < 2)
                {
                    if(MainTank[0] + ReserveTank[0] >= MainTank[1])
                    {
                        e.Graphics.DrawString(tempText, prozentFont, Brushes.Red, 60, 130);
                    }
                }
                else
                {
                    if (MainTank[0] >= MainTank[1] + ReserveTank[0])
                    {
                        e.Graphics.DrawString(tempText, prozentFont, Brushes.Red, 60, 130);
                    }
                }
            }

        }
    }
}
