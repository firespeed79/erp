﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Diagnostics;
using MainProgram.bus;
using MainProgram.model;

namespace MainProgram
{
    public partial class FormMaterielOutEarningsOrder : Form
    {
        private int m_staffSavePkey = -1;
        private int m_materielOutStaffPkey = -1;
        private string m_billNumber = "";
        private readonly int BillTypeNumber = 15;
        private readonly int DateGridVeiwListDataListRowCount = FormMain.DATA_GRID_VIEW_DEFAULT_ROW_COUNT;
        private int m_rowIndex = -1, m_columnIndex = -1;
        private bool m_isInit = false;
        private bool m_isRedBill = false;
        private bool m_isSaveSuccess = false;

        public DataGridViewTextBoxEditingControl CellEdit = null;
        BillDataGridViewExtend m_dateGridVeiwListDataList = new BillDataGridViewExtend();
        DataGridViewExtend m_dateGridVeiwListDataCount = new DataGridViewExtend();
        MaterielOutEarningsOrderTable m_materieOutOrder = new MaterielOutEarningsOrderTable();

        public enum DataGridColumnName
        {
            RowNum,
            MatetielNumber,
            MatetielName,
            Brand,
            Model,
            Unit,
            Value,
            Price,
            Turnover,
            Note
        };

        public FormMaterielOutEarningsOrder(string billNumber = "")
        {
            InitializeComponent();
            m_billNumber = billNumber;
        }

        private void FormMaterielOutEarningsOrder_Load(object sender, EventArgs e)
        {
            // DataGridView初始化
            dataGridViewInit();

            if (m_billNumber.Length == 0)
            {
                // 单据号
                this.labelBillNumber.Text = BillNumber.getInctance().getNewBillNumber(BillTypeNumber, DateTime.Now.ToString("yyyy-MM-dd"));
                
                // 制单人初始化
                this.labelMakeBillStaff.Visible = true;
                this.labelMakeBillStaff.Text = DbPublic.getInctance().getCurrentLoginUserName();
            }
            else 
            {
                readBillInfoToUI();
            }

            setPageActionEnable();
        }

        private void dataGridViewInit()
        {
            // 物料资料初始化
            m_dateGridVeiwListDataList.addDataGridViewColumn("行号", 55, true, true);
            m_dateGridVeiwListDataList.addDataGridViewColumn("物料ID\\编码(*)", 100, true, false);

            if (DateGridVeiwListDataListRowCount > 12)
            {
                m_dateGridVeiwListDataList.addDataGridViewColumn("物料名称", 144, true, true);
            }
            else
            {
                m_dateGridVeiwListDataList.addDataGridViewColumn("物料名称", 161, true, true);
            }

            m_dateGridVeiwListDataList.addDataGridViewColumn("品牌", 60, true, true);
            m_dateGridVeiwListDataList.addDataGridViewColumn("型号", 63, true, true);
            m_dateGridVeiwListDataList.addDataGridViewColumn("基本\n单位", 100, true, true);
            m_dateGridVeiwListDataList.addDataGridViewColumn("数量(*)", 100, true, false);
            m_dateGridVeiwListDataList.addDataGridViewColumn("单价", 80, true, true);
            m_dateGridVeiwListDataList.addDataGridViewColumn("金额", 80, true, true);
            m_dateGridVeiwListDataList.addDataGridViewColumn("备注", 203, true, false);

            m_dateGridVeiwListDataList.initDataGridViewColumn(this.dataGridViewDataList);
            m_dateGridVeiwListDataList.initDataGridViewData(DateGridVeiwListDataListRowCount);

            // 合计行DataGridView初始化
            SortedDictionary<int, DataGridViewColumnInfoStruct> columnsInfo = m_dateGridVeiwListDataList.getDataGridViewColumns();
            for (int i = 0; i < columnsInfo.Count; i++)
            {
                DataGridViewColumnInfoStruct column = new DataGridViewColumnInfoStruct();
                column = (DataGridViewColumnInfoStruct)columnsInfo[i];
                m_dateGridVeiwListDataCount.addDataGridViewColumn(column.headerText, column.Width, column.isVisiable);
            }

            SortedDictionary<int, ArrayList> dataList = new SortedDictionary<int, ArrayList>();

            ArrayList record = new ArrayList();
            record.Add("合计");
            dataList.Add(dataList.Count, record);

            m_dateGridVeiwListDataCount.initDataGridViewColumn(this.dataGridViewDataCount);
            m_dateGridVeiwListDataCount.initDataGridViewData(dataList);

            this.dataGridViewDataCount.ColumnHeadersVisible = false;
            this.dataGridViewDataCount.Rows[0].DefaultCellStyle.BackColor = System.Drawing.Color.LightSkyBlue;

            // 初始化完毕
            m_isInit = true;
        }

        #region 供应日期
        private void panelDateTime_Click(object sender, EventArgs e)
        {
            if (m_materieOutOrder.isReview == "1")
            {
                return;
            }

            if (!this.dateTimePickerTradingDate.Visible)
            {
                this.panelTradingDate.Visible = false;
                this.labelTradingDate.Visible = false;
                this.dateTimePickerTradingDate.Visible = true;
                this.dateTimePickerTradingDate.Focus();
            }
        }
        private void dateTimePicker1_Leave(object sender, EventArgs e)
        {
            this.panelTradingDate.Visible = true;

            this.labelTradingDate.Visible = true;

            this.dateTimePickerTradingDate.Visible = false;
            this.labelTradingDate.Text = this.dateTimePickerTradingDate.Value.ToString("yyyy-MM-dd");
        }
        #endregion

        #region 保管员
        private void panelSave_Click(object sender, EventArgs e)
        {
            if (!this.textBoxSave.Visible)
            {
                this.labelSave.Visible = false;
                this.textBoxSave.Visible = true;
                this.textBoxSave.Focus();
            }
        }

        private void panelSave_DoubleClick(object sender, EventArgs e)
        {
            if (!this.textBoxSave.Visible)
            {
                this.labelSave.Visible = false;
                this.textBoxSave.Visible = true;
            }
            else
            {
                FormBaseStaff fbs = new FormBaseStaff(true);
                fbs.ShowDialog();

                m_staffSavePkey = fbs.getSelectRecordPkey();
                StaffTable record = Staff.getInctance().getStaffInfoFromPkey(m_staffSavePkey);
                this.textBoxSave.Text = record.name;
                this.textBoxSave.Visible = true;
            }
        }

        private void textBoxSave_Leave(object sender, EventArgs e)
        {
            this.textBoxSave.Visible = false;
            this.labelSave.Text = this.textBoxSave.Text.ToString();
            this.labelSave.Visible = this.textBoxSave.Text.Length > 0;
        }
        #endregion

        #region 领料人
        private void panelVerify_Click(object sender, EventArgs e)
        {
            if (!this.textBoxVerify.Visible)
            {
                this.labelVerify.Visible = false;
                this.textBoxVerify.Visible = true;
                this.textBoxVerify.Focus();
            }
        }

        private void panelVerify_DoubleClick(object sender, EventArgs e)
        {
            if (!this.textBoxVerify.Visible)
            {
                this.labelVerify.Visible = false;
                this.textBoxVerify.Visible = true;
            }
            else
            {
                FormBaseStaff fbs = new FormBaseStaff(true);
                fbs.ShowDialog();
                
                m_materielOutStaffPkey = fbs.getSelectRecordPkey();
                StaffTable record = Staff.getInctance().getStaffInfoFromPkey(m_materielOutStaffPkey);
                this.textBoxVerify.Text = record.name;
                this.textBoxVerify.Visible = true;
            }
        }

        private void textBoxVerify_Leave(object sender, EventArgs e)
        {
            this.textBoxVerify.Visible = false;
            this.labelVerify.Text = this.textBoxVerify.Text.ToString();
            this.labelVerify.Visible = this.textBoxVerify.Text.Length > 0;
        }
        #endregion

        private void save_Click(object sender, EventArgs e)
        {
            m_isSaveSuccess = false;

            if ((sender.ToString() == "保存" || sender.ToString() == "审核") &&
                MaterielOutEarningsOrder.getInctance().checkBillIsReview(this.labelBillNumber.Text.ToString()))
            {
                MessageBoxExtend.messageWarning("单据已被审核，所有数据无法进行更改，无法重复保存或审核\r\n请重新登录或手动刷新后查看单据详情");
                return;
            }

            this.ActiveControl = this.toolStrip1;

            // 得到详细的销售信息
            ArrayList dataList = getMaterielOutEarningsOrderDetailsValue();

            if (dataList.Count > 0)
            {
                // 销售订单表头和表尾信息
                MaterielOutEarningsOrderTable record = getMaterielOutEarningsOrderValue();
                if (orderInfoIsFull(record) && orderDetailsIsFull(dataList))
                {
                    MaterielOutEarningsOrder.getInctance().insert(record, false);
                    MaterielOutEarningsOrderDetails.getInctance().insert(dataList);
                    BillNumber.getInctance().inserBillNumber(BillTypeNumber, this.labelTradingDate.Text, this.labelBillNumber.Text.ToString());

                    m_isSaveSuccess = true;

                    if (m_billNumber.Length == 0)
                    {
                        MessageBoxExtend.messageOK("数据保存成功");
                    }

                    this.Close();
                }
            }
            else 
            {
                MessageBoxExtend.messageWarning("此单据不包含任何交易信息，单据保存失败.");
            }
        }

        private MaterielOutEarningsOrderTable getMaterielOutEarningsOrderValue()
        {
            MaterielOutEarningsOrderTable record = new MaterielOutEarningsOrderTable();

            record.tradingDate = this.labelTradingDate.Text;
            record.billNumber = this.labelBillNumber.Text;

            record.sumValue = this.dataGridViewDataCount.Rows[0].Cells[(int)DataGridColumnName.Value].Value.ToString();
            record.sumMoney = this.dataGridViewDataCount.Rows[0].Cells[(int)DataGridColumnName.Turnover].Value.ToString();

            record.staffSaveId = m_staffSavePkey;
            record.materielOutStaffId = m_materielOutStaffPkey;

            if (m_billNumber.Length == 0)
            {
                record.makeOrderStaff = DbPublic.getInctance().getCurrentLoginUserID();
            }
            else 
            {
                record.makeOrderStaff = m_materieOutOrder.makeOrderStaff;
            }

            if (m_isRedBill)
            {
                record.isRedBill = 1;
            }
            else
            {
                record.isRedBill = 0;
            }

            return record;
        }

        private ArrayList getMaterielOutEarningsOrderDetailsValue()
        {
            ArrayList list = new ArrayList();

            for (int rowIndex = 0; rowIndex < DateGridVeiwListDataListRowCount; rowIndex++)
            {
                if (this.dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.MatetielNumber].Value.ToString().Length == 0)
                {
                    break;
                }
                else 
                {
                    MaterielOutEarningsOrderDetailsTable record = new MaterielOutEarningsOrderDetailsTable();

                    record.billNumber = this.labelBillNumber.Text;
                    record.rowNumber = dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.RowNum].Value.ToString();
                    record.materielID = Convert.ToInt32(dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.MatetielNumber].Value.ToString());
                    record.price = Convert.ToDouble(dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Price].Value.ToString());
                    record.value = Convert.ToDouble(dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Value].Value.ToString());
                    record.note = dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Note].Value.ToString();

                    list.Add(record);
                }
            }

            return list;
        }

        private bool orderInfoIsFull(MaterielOutEarningsOrderTable record)
        {
            if (record.tradingDate.Length == 0)
            {
                MessageBoxExtend.messageWarning("日期不完整，单据保存失败");
                return false;
            }

            if (record.materielOutStaffId == -1)
            {
                MessageBoxExtend.messageWarning("领料人信息不完整，单据保存失败");
                return false;
            }

            return true;
        }

        private bool orderDetailsIsFull(ArrayList list)
        {
            bool isRet = true;

            for (int rowIndex = 0; rowIndex < list.Count; rowIndex++)
            {
                MaterielOutEarningsOrderDetailsTable record = new MaterielOutEarningsOrderDetailsTable();
                record = (MaterielOutEarningsOrderDetailsTable)list[rowIndex];

                if (record.price == 0)
                {
                    MessageBoxExtend.messageWarning("第[" + record.rowNumber + "]信息中物料单价不能为空");
                    isRet = false;
                    break;
                }

                if (record.value == 0)
                {
                    MessageBoxExtend.messageWarning("第[" + record.rowNumber + "]信息中物料数量不能为空");
                    isRet = false;
                    break;
                }
            }

            return isRet;
        }

        private void toolStripButtonReview_Click(object sender, EventArgs e)
        {
            try
            {
                save_Click(sender, e);

                if (m_isSaveSuccess)
                {
                    MaterielOutEarningsOrder.getInctance().billReview(m_billNumber, m_isRedBill);
                }
            }
            catch (Exception exp)
            {
                MessageBoxExtend.messageError(exp.ToString());
            }
        }

        private void printDisplay_Click(object sender, EventArgs e)
        {           
            // PrintBmpFile.getInctance().printCurrentWin(Width, Height, this.Location.X, this.Location.Y, true);
            if (m_billNumber.Length > 0)
            {
                FormOrderPrint fop = new FormOrderPrint(BillTypeNumber, m_billNumber, this.dataGridViewDataList);
                fop.ShowDialog();
            }
            else
            {
                MessageBoxExtend.messageWarning("请先保存数据再打印");
            }
        }

        private void print_Click(object sender, EventArgs e)
        {
            PrintBmpFile.getInctance().printCurrentWin(Width, Height, this.Location.X, this.Location.Y);
        }

        private void selectSourceOrder_Click(object sender, EventArgs e)
        {
            if (m_rowIndex != -1 && m_columnIndex == (int)DataGridColumnName.MatetielNumber)
            {
                FormBaseMateriel fbm = new FormBaseMateriel(true);
                fbm.ShowDialog();
                this.dataGridViewDataList.Rows[m_rowIndex].Cells[m_columnIndex].Value = Convert.ToString(fbm.getSelectRecordPkey());
                this.dataGridViewDataList.CurrentCell = this.dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Value];
            }
        }

        private void calculator_Click(object sender, EventArgs e)
        {
            Process.Start("Calc");
        }

        private void close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void dataGridViewDataList_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex == (int)DataGridColumnName.MatetielNumber)
            {
                //验证DataGridView是否又空的行
                int nullRowNumber = m_dateGridVeiwListDataList.getExistNullRow(e.RowIndex);

                if (nullRowNumber != -1)
                {
                    MessageBoxExtend.messageWarning("行号[" + Convert.ToString(nullRowNumber + 1) + "]数据为空，请现在空行中输入");
                    dataGridViewDataList.CurrentCell = this.dataGridViewDataList.Rows[nullRowNumber].Cells[(int)DataGridColumnName.MatetielNumber];

                    m_rowIndex = nullRowNumber;
                    m_columnIndex = e.ColumnIndex;

                    return;
                }
            }

            m_rowIndex = e.RowIndex;
            m_columnIndex = e.ColumnIndex;
        }

        private void dataGridViewDataList_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == (int)DataGridColumnName.MatetielNumber)
            {
                // 当用户选择的相应物料后，自动对DataGridView行某些值进行赋值
                if (dataGridViewDataList.Rows[m_rowIndex].Cells[m_columnIndex].EditedFormattedValue.ToString().Length > 0)
                {
                    setMatetielInfoToDataGridView(dataGridViewDataList.Rows[m_rowIndex].Cells[m_columnIndex].EditedFormattedValue.ToString());
                }
            }
            else if (e.ColumnIndex == (int)DataGridColumnName.Price || e.ColumnIndex == (int)DataGridColumnName.Value)
            {
                // 当单价和数量有变化时，自动计算物料金额
                setTurnoverInfoDataGridView();
            }
        }
        
        private void setMatetielInfoToDataGridView(string id)
        {
            /* 如果是物料编码列，需要判断该物料编码是否存在
            * 如果存在读取相应的值填充DataGridView中对应的其他列，如果不存在该物料编码，则清空该行
            * */
            double pkey = 0;
            //使用这个输入的值，匹配物料编号
            MaterielTable record = Materiel.getInctance().getMaterielInfoFromNum(Convert.ToString(id));

            if (record == null || id.ToLower() != record.num.ToLower() || record.pkey == 0)
            {
                try
                {
                    //使用这个输入的值，匹配物料key
                    pkey = Convert.ToDouble(id.ToString());

                    record = Materiel.getInctance().getMaterielInfoFromPkey((int)pkey);

                    if (pkey != record.pkey || record.pkey == 0)
                    {
                        MessageBoxExtend.messageWarning("[" + dataGridViewDataList.Rows[m_rowIndex].Cells[m_columnIndex].EditedFormattedValue.ToString() +
                            "]不存在，请重新输入或选择");
                        m_dateGridVeiwListDataList.clearDataGridViewRow(m_rowIndex);

                        return;
                    }
                }
                catch
                {
                    MessageBoxExtend.messageWarning("[" + dataGridViewDataList.Rows[m_rowIndex].Cells[m_columnIndex].EditedFormattedValue.ToString() +
                        "]不存在，请重新输入或选择");
                    m_dateGridVeiwListDataList.clearDataGridViewRow(m_rowIndex);

                    return;
                }
            }

            InitMaterielTable MaterielCountdata = InitMateriel.getInctance().getMaterielInfoFromMaterielID((int)pkey);

            dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.MatetielNumber].Value = record.pkey;
            dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.MatetielName].Value = record.name;
            dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Brand].Value = record.brand;
            dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Model].Value = record.model;
            dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Unit].Value =
            AuxiliaryMaterial.getInctance().getAuxiliaryMaterialNameFromPkey("BASE_UNIT_LIST", record.unitSale);
            dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Price].Value = Convert.ToString(MaterielCountdata.price);
            dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Value].Value = "0";
            dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Turnover].Value = "0";
        }

        private void setTurnoverInfoDataGridView()
        {
            if (dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Price].Value.ToString().Length > 0 &&
                dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Value].Value.ToString().Length > 0)
            {
                double price = Convert.ToDouble(dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Price].Value.ToString());
                double value = Convert.ToDouble(dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Value].Value.ToString());
                double turnover = price * value;

                // 金额信息保留2位小数儿
                turnover = (double)(Math.Round(turnover * 100)) / 100;

                dataGridViewDataList.Rows[m_rowIndex].Cells[(int)DataGridColumnName.Turnover].Value = Convert.ToString(turnover);
            }
        }

        private void dataGridViewDataList_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            CellEdit = (DataGridViewTextBoxEditingControl)e.Control;
            CellEdit.SelectAll();
            CellEdit.KeyPress += Cells_KeyPress; // 绑定到事件
        }

        private void Cells_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (m_columnIndex != (int)DataGridColumnName.Note && m_columnIndex != (int)DataGridColumnName.MatetielNumber)
            {
                e.Handled = m_dateGridVeiwListDataList.isValidDataGridViewCellValue(e.KeyChar,
                    this.dataGridViewDataList.Rows[m_rowIndex].Cells[m_columnIndex].EditedFormattedValue.ToString());
            }
            else 
            {
                e.Handled = false;
            }
        }

        private void dataGridViewDataList_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (m_isInit)
            {
                if (e.ColumnIndex == (int)DataGridColumnName.Value || e.ColumnIndex == (int)DataGridColumnName.Turnover)
                {
                    ArrayList columns = new ArrayList();

                    columns.Add((int)DataGridColumnName.Value);
                    columns.Add((int)DataGridColumnName.Turnover);

                    for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                    {
                        int columnNumber = (int)columns[columnIndex];
                        double sum = 0;

                        for (int rowIndex = 0; rowIndex < DateGridVeiwListDataListRowCount; rowIndex++)
                        {
                            if (this.dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.MatetielNumber].Value.ToString().Length > 0)
                            {
                                if (this.dataGridViewDataList.Rows[rowIndex].Cells[columnNumber].Value.ToString().Length > 0)
                                {
                                    sum += Convert.ToDouble(this.dataGridViewDataList.Rows[rowIndex].Cells[columnNumber].Value.ToString());
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        this.dataGridViewDataCount.Rows[0].Cells[columnNumber].Value = Convert.ToString(sum);
                    }
                }
            }
        }

        private void readBillInfoToUI()
        {
            // 单据表头表尾信息
            m_materieOutOrder = MaterielOutEarningsOrder.getInctance().getMaterielOutEarningsOrderInfoFromBillNumber(m_billNumber);

            m_staffSavePkey = m_materieOutOrder.staffSaveId;
            m_materielOutStaffPkey = m_materieOutOrder.materielOutStaffId;

            this.labelTradingDate.Visible = true;
            this.labelBillNumber.Visible = true;
            this.labelMakeBillStaff.Visible = true;
            this.labelReviewBillStaff.Visible = true;
            this.labelReviewDate.Visible = true;
            this.labelSave.Visible = true;
            this.labelVerify.Visible = true;
            
            this.labelTradingDate.Text = m_materieOutOrder.tradingDate;
            this.labelBillNumber.Text = m_materieOutOrder.billNumber;
            this.labelMakeBillStaff.Text = m_materieOutOrder.makeOrderStaffName;
            this.labelSave.Text = m_materieOutOrder.staffSaveName;
            this.labelVerify.Text = m_materieOutOrder.materielOutStaffName;

            // DataGridView 赋值
            writeBillDetailsInfoFromBillNumber(m_billNumber);

            // 如果单据已审核，则禁用页面所有控件
            if (m_materieOutOrder.isReview == "1")
            {
                this.labelReviewBillStaff.Text = m_materieOutOrder.orderrReviewName;
                this.labelReviewDate.Text = m_materieOutOrder.reviewDate;
                this.panelIsReview.Visible = true;

                this.save.Enabled = false;
                this.toolStripButtonReview.Enabled = false;
                this.panelTradingDate.Visible = false;

                this.dateTimePickerTradingDate.Visible = false;

                this.dataGridViewDataList.ReadOnly = true;
                this.dataGridViewDataCount.ReadOnly = true;
            }
            else
            {
                this.labelReviewBillStaff.Visible = false;
                this.labelReviewDate.Visible = false;
            }

            if (m_materieOutOrder.isRedBill == 1)
            {
                m_isRedBill = true;
                this.panelRed.Visible = true;
            }
        }

        private void writeBillDetailsInfoFromBillNumber(string billNumber)
        {
            // DataGridView 赋值
            SortedDictionary<int, MaterielOutEarningsOrderDetailsTable> orderDetails =
                MaterielOutEarningsOrderDetails.getInctance().getMaterielOutEarningsInfoFromBillNumber(billNumber);

            foreach (KeyValuePair<int, MaterielOutEarningsOrderDetailsTable> index in orderDetails)
            {
                MaterielOutEarningsOrderDetailsTable record = new MaterielOutEarningsOrderDetailsTable();
                record = index.Value;

                int rowIndex = Convert.ToInt32(record.rowNumber.ToString()) - 1;
                MaterielTable materielInfo = Materiel.getInctance().getMaterielInfoFromPkey(record.materielID);

                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.MatetielNumber].Value = record.materielID;
                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.MatetielName].Value = record.materielName;
                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Brand].Value = materielInfo.brand;
                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Model].Value = record.materielModel;
                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Unit].Value = record.materielUnitSale;
                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Price].Value = record.price;
                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Value].Value = record.value;
                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Turnover].Value = record.sumMoney;
                dataGridViewDataList.Rows[rowIndex].Cells[(int)DataGridColumnName.Note].Value = record.note;
            }
        }

        private void redBill_Click(object sender, EventArgs e)
        {
            m_isRedBill = true;
            this.panelRed.Enabled = true;
            this.panelRed.Visible = true;

            this.redBill.ForeColor = Color.Red;
            this.redBill.CheckState = CheckState.Checked;
            this.blueBill.CheckState = CheckState.Unchecked;
        }

        private void blueBill_Click(object sender, EventArgs e)
        {
            m_isRedBill = false;
            this.panelRed.Enabled = false;
            this.panelRed.Visible = false;

            this.redBill.ForeColor = Color.Black;
            this.redBill.CheckState = CheckState.Unchecked;
            this.blueBill.CheckState = CheckState.Checked;
        }

        private void setPageActionEnable()
        {
            SortedDictionary<int, ActionTable> list = MainProgram.model.Action.getInctance().getActionInfoFromModuleID(305);

            foreach (KeyValuePair<int, ActionTable> index in list)
            {
                object activeObject = this.GetType().GetField(index.Value.uiActionName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase).GetValue(this);

                bool isEnable = AccessAuthorization.getInctance().isAccessAuthorization(index.Value.pkey,
                    Convert.ToString(DbPublic.getInctance().getCurrentLoginUserID()));

                if (activeObject != null)
                {
                    UserInterfaceActonState.setUserInterfaceActonState(activeObject,
                        ((System.Reflection.MemberInfo)(activeObject.GetType())).Name.ToString(), isEnable);
                }
            }
        }

        private void dataGridViewDataList_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Right) && (e.RowIndex >= 0 && e.RowIndex < DateGridVeiwListDataListRowCount))
            {
                m_rowIndex = e.RowIndex;

                contextMenuStripDataGridView.Show(MousePosition.X, MousePosition.Y);
            }
        }

        private void ToolStripMenuItemDelRow_Click(object sender, EventArgs e)
        {
            if (dataGridViewDataList.Rows[m_rowIndex].Cells[1].Value.ToString().Length > 0)
            {
                string rowNum = dataGridViewDataList.Rows[m_rowIndex].Cells[0].Value.ToString();

                if (MessageBoxExtend.messageQuestion("确认删除第" + rowNum + "行的数据吗？"))
                {
                    m_dateGridVeiwListDataList.delDataGridVewRow(Convert.ToInt32(rowNum), DateGridVeiwListDataListRowCount);
                }
            }
            else
            {
                MessageBoxExtend.messageWarning("选择行的物料ID为空, 请重新选择");
            }
        }
    }
}