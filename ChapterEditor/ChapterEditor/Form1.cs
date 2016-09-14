using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;


//引用 MySqL
using MySql.Data.MySqlClient;
using System.Drawing.Drawing2D;

namespace ChapterEditor
{
    public partial class Form1 : Form
    {
        //create a datascture to store the PictureObject
        //arraylist
        ArrayList Data = new ArrayList();

        //创建数据结构，用于已经绘制的图形的保存
        ArrayList ImageHasDraw = new ArrayList();

        //创建用于中转的DataScture
        DataStructure Trans = new DataStructure();

        int NowPage = 1;    //当前页数
        int page = 1;       //总页数
        int StartIndexOfPage = 0;  //当前页数所对应的开始索引
        int EndIndexOfPage = 0;    //当前页数所对应的结束索引

        Graphics g;            //声明画布，用于传入panel中的画布对象
        Bitmap bitmap;         //双缓冲时的bitmap对象.

        /*创建结构体--核心数据结构
           bitmap对象
           场景中的位置---与文本框绑定
           ArrayList对象--保存着相应的结构体 ，用于保存子对象*/
        public struct DataStructure
        {
            public Bitmap Image;
            public String ImageName;
            public Point ImageLocation;
            public ArrayList ChildObjects;
            public int Index;
            public bool isColider;
        }

        ArrayList IndexForMerge = new ArrayList();
        Bitmap BitBuffer; //用于中转的Bitmap对象。
        bool CanDraw = false;
        bool CanStartImproveArrayIndex = false;

        /**********************************************/
        Point MouseLocation;  //鼠标位置
        Size GraphicsRegion;   //绘图区的大小
        Point PicBoxLocation;  //容器位置
        Size PicBoxRegion;   //容器大小
        Point WindowsLocation;  //窗口位置
        Point ScrollLocation;   //滚动条的位置
        /**********************************************/
        Point GraphicsLocation;  //当前要绘制的位置

        public Form1()
        {
            InitializeComponent();
            //创建画布,panel滚动条的自动感应是应用与容器的。
            g = pictureBox5.CreateGraphics();
            PictureRegion.AutoScroll = true;

        }
        //绘制函数
        private void PictureRegion_Paint(object sender, PaintEventArgs e)
        {

        }

        private void 导入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog op1 = new OpenFileDialog();
                op1.Multiselect = true;
                op1.Filter = "PNG文件(*.png)|*.PNG|JPG文件(*.jpg)|*.jpg";
                string[] filenames;        //保存文件名的ArrayList
                if (op1.ShowDialog() == DialogResult.OK)
                {
                    filenames = op1.FileNames;
                }
                else
                {
                    filenames = null;
                }
                for (int i = 0; i <= filenames.Length - 1; i++)
                {
                    //将结构体补充完整，然后添加到Data中
                    DataStructure a;
                    a.Image = new Bitmap(filenames[i]);
                    a.ImageName = filenames[i];
                    a.Index = 0;
                    Point b = new Point(0, 0);
                    a.ImageLocation = b;
                    a.isColider = false;
                    a.ChildObjects = null;
                    //add the picture in the Data
                    Data.Add(a);
                    textBox1.Text = Data.Count.ToString();
                    //只要最后一位的bitmap

                    Bitmap bt = ((DataStructure)Data[Data.Count - 1]).Image;
                    Bitmap bt1 = getBitMap(bt);  //转换大小
                    SetPicBoxPicture(ref pictureBox1, ref pictureBox2, ref pictureBox3, ref pictureBox4, bt1);   //图片从data--》picBOX
                    UpdatePage(Data);   //更新页数
                    UpdateDataCount(Data);  //更新List总数目
                }

            }
            catch (System.NullReferenceException ex)
            {

            }
            
        }

        private void PictureBOX_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            //边界判断
            if (NowPage == 1)
                return;
            //更新当前页数
            reduceNowPage();
            ChangePage(ref pictureBox1, ref pictureBox2, ref pictureBox3, ref pictureBox4, Data);
            //更新3d状态
            UpdateBorderStyle();
        }
        //更新3d状态
        private void UpdateBorderStyle()
        {
            PictureBox[] pic = { pictureBox1, pictureBox2, pictureBox3, pictureBox4 };
            for (int i = 0; i <= 3; i++)
            {
                pic[i].BorderStyle = BorderStyle.None;
            }
        }

        //添加图像  将data中的图片设置到PictureBox中
        private int SetPicBoxPicture(ref PictureBox pic1, ref PictureBox pic2, ref PictureBox pic3, ref PictureBox pic4, Bitmap bt)
        {
            //传引用的方式
            PictureBox[] pic = { pic1, pic2, pic3, pic4 };
            for (int i = 0; i <= pic.Length - 1; i++)
            {
                if (pic[i].Image == null)
                {
                    pic[i].Image = bt;
                    return 0;
                }
            }
            return 0;
        }

        //快捷导入
        private void button7_Click(object sender, EventArgs e)
        {
            导入ToolStripMenuItem_Click(sender, e);
        }

        //点击后3d--none  ；  none---3d
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (pictureBox1.BorderStyle == BorderStyle.None)
            {
                pictureBox1.BorderStyle = BorderStyle.Fixed3D;
            }
            OnlyOnePicBox3d(0);
        }
        private void GetBitmapFromPicBoxIndex()
        {
            //逐个判断borderStyle的状态，是3d的则将中转用的bitmap设置为其 Image
            PictureBox[] pic = { pictureBox1, pictureBox2, pictureBox3, pictureBox4 };
            for (int i = 0; i <= 3; i++)
            {
                if (pic[i].BorderStyle == BorderStyle.Fixed3D && pic[i].Image != null)
                {
                    int index = (NowPage - 1) * 4 + i;
                    BitBuffer = ((DataStructure)Data[index]).Image;
                    Trans = (DataStructure)Data[index];
                    //test;
                    CanDraw = true;
                    return;
                }
            }
            CanDraw = false;
            return;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //TreeView1添加根节点：
            treeView1.LabelEdit = false;//不可编辑
            //添加根结点
            TreeNode root = new TreeNode();
            root.Text = "All Objects";
            treeView1.Nodes.Add(root);


        }
        //更新pages数目
        private void UpdatePage(ArrayList Data)
        {

            if (Data.Count % 4 != 0)
            { page = (int)(Data.Count / 4) + 1; }
            else
            {
                page = (int)(Data.Count / 4);
                if (page == 0)
                {
                    page = 1;
                }
            }

            label3.Text = "页数：" + page.ToString();
        }
        //更新Data中数目
        private void UpdateDataCount(ArrayList Data)
        {
            label2.Text = "List总数目：" + Data.Count.ToString();
        }
        //改变当前页面内容
        private void ChangePage(ref PictureBox pic1, ref PictureBox pic2, ref PictureBox pic3, ref PictureBox pic4, ArrayList Data)
        {
            label4.Text = "当前页数：" + NowPage.ToString();
            //更新当前索引
            StartIndexOfPage = (NowPage - 1) * 4;
            EndIndexOfPage = StartIndexOfPage + 3;
            if (EndIndexOfPage > Data.Count - 1)
                EndIndexOfPage = Data.Count - 1;
            PictureBox[] pic = { pictureBox1, pictureBox2, pictureBox3, pictureBox4 };
            int i, j = 0;
            for (i = StartIndexOfPage; i <= EndIndexOfPage; i++)
            {
                pic[j].Image = getBitMap(((DataStructure)Data[i]).Image);
                j++;

            }
            //销毁剩余的图像
            if (NowPage == page)
            {
                while (j <= 3)
                {
                    pic[j].Image = null;
                    j++;
                }
            }
        }
        //增加当前页数
        private void AddNowPage()
        {
            NowPage++;
        }
        //减少当前页数
        private void reduceNowPage()
        {
            NowPage--;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (page == 1)
                return;
            if (NowPage == page)
                return;
            //更新当前页数
            AddNowPage();
            //边界判断

            ChangePage(ref pictureBox1, ref pictureBox2, ref pictureBox3, ref pictureBox4, Data);
            UpdateBorderStyle();
        }
        //获取pictureBOX大小的图像
        private Bitmap getBitMap(Bitmap bt)
        {
            return new Bitmap(bt, pictureBox1.Size);
        }

        private void del1_Click(object sender, EventArgs e)
        {
            //设置边界
            if (Data.Count == 0 || pictureBox1.Image == null)
                return;
            int NowIndex = (NowPage - 1) * 4;     //获取当前索引
            //更新Data
            Data.RemoveAt(NowIndex);
            UpdatePage(Data);
            NowPageTest();
            //更新当前页面
            UpdateNowPageContent();
        }
        private void UpdateNowPageContent()
        {
            //更新索引
            StartIndexOfPage = (NowPage - 1) * 4;
            EndIndexOfPage = StartIndexOfPage + 3;
            if (EndIndexOfPage > Data.Count - 1)
                EndIndexOfPage = Data.Count - 1;
            PictureBox[] pic = { pictureBox1, pictureBox2, pictureBox3, pictureBox4 };
            int i, j = 0;
            for (i = StartIndexOfPage; i <= EndIndexOfPage; i++)
            {
                pic[j].Image = getBitMap(((DataStructure)Data[i]).Image);
                j++;

            }
            //销毁剩余的图像
            if (NowPage == page)
            {
                while (j <= 3)
                {
                    pic[j].Image = null;
                    j++;
                }
            }
            label4.Text = "当前页数：" + NowPage.ToString();
            UpdateDataCount(Data);
        }

        private void del2_Click(object sender, EventArgs e)
        {
            //设置边界
            if (Data.Count == 0 || pictureBox2.Image == null)
                return;
            int NowIndex = (NowPage - 1) * 4 + 1;     //获取当前索引
            //更新Data
            Data.RemoveAt(NowIndex);
            UpdatePage(Data);
            NowPageTest();
            //更新当前页面
            UpdateNowPageContent();
        }

        private void del3_Click(object sender, EventArgs e)
        {
            //设置边界
            if (Data.Count == 0 || pictureBox2.Image == null)
                return;
            int NowIndex = (NowPage - 1) * 4 + 2;     //获取当前索引
            //更新Data
            Data.RemoveAt(NowIndex);
            UpdatePage(Data);
            NowPageTest();
            //更新当前页面
            UpdateNowPageContent();
        }

        private void del4_Click(object sender, EventArgs e)
        {
            //设置边界
            if (Data.Count == 0 || pictureBox3.Image == null)
                return;
            int NowIndex = (NowPage - 1) * 4 + 3;     //获取当前索引
            //更新Data
            Data.RemoveAt(NowIndex);
            UpdatePage(Data);
            NowPageTest();
            //更新当前页面
            UpdateNowPageContent();
        }
        //NowPage与 page 边界检测函数
        private void NowPageTest()
        {
            if (NowPage > page)
            {
                NowPage--;
                UpdateNowPageContent();
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                //操作ImageHasDraw数据结构，集合操作
                int index1 = int.Parse(IndexForMerge[0].ToString());
                DataStructure ParentNode1 = (DataStructure)ImageHasDraw[index1];
                for (int i = 1; i <= IndexForMerge.Count - 1; i++)
                {
                    index1 = int.Parse(IndexForMerge[i].ToString());
                    DataStructure ds1 = (DataStructure)ImageHasDraw[index1];
                    ParentNode1.ChildObjects.Add(ds1);
                }
                //更新ImageHasDraw

                //1，建立要删除的index数组
                //2，从大到小排序index
                //3，删除一个，index内部指针+1
                //4，重建一个ImageHasDraw，去除残余空间
                //1
                int[] indexToDelete = new int[IndexForMerge.Count - 1];
                for (int i = 1; i <= IndexForMerge.Count - 1; i++)
                {
                    indexToDelete[i - 1] = int.Parse(IndexForMerge[i].ToString());
                }
                //2
                Array.Sort(indexToDelete);
                Array.Reverse(indexToDelete);
                //3
                for (int i = 0; i <= indexToDelete.Length - 1; i++)
                {
                    ImageHasDraw.RemoveAt(indexToDelete[i]);
                }
                //4
                ArrayList NewImageHasDraw = new ArrayList();
                for (int i = 0; i <= ImageHasDraw.Count - 1; i++)
                {
                    NewImageHasDraw.Add(ImageHasDraw[i]);
                }
                ImageHasDraw.Clear();
                ImageHasDraw = NewImageHasDraw;


                //更新ImageHasDraw索引
                //递归树更新
                for (int i = 0; i <= ImageHasDraw.Count - 1; i++)
                {
                    DataStructure ds = (DataStructure)ImageHasDraw[i];
                    ds.Index = i;
                    ImageHasDraw[i] = ds;                //值传递陷阱。 但是为什么偏偏在此地发生。此处获取的是整个ds对象。
                    UpdateImageHasDrawIndex(ds);
                }

                //更新TreeView1
                UpdateTreeView();
                //清空 IndexForMerge
                IndexForMerge.Clear();
            } catch (System.ArgumentOutOfRangeException ex)
            {
                MessageBox.Show("TreeView中无可用对象");
            }

        }
        //递归更新childObjects.Index
        private void UpdateImageHasDrawIndex(DataStructure ds)
        {
            if (ds.ChildObjects != null)
            {
                for (int j = 0; j <= ds.ChildObjects.Count - 1; j++)
                {
                    DataStructure ds1 = (DataStructure)ds.ChildObjects[j];
                    ds1.Index = j;
                    ds.ChildObjects[j] = ds1;
                    UpdateImageHasDrawIndex(ds1);
                }
            }

        }
        //清除绘图区的方法
        private void ClearAll()
        {
            bitmap = new Bitmap(GraphicsRegion.Width, GraphicsRegion.Height);   //create a bitmap
            g = Graphics.FromImage(bitmap);    //get the Graphics ref from the bitmap  
            //绘制边框
            Pen pen = new Pen(new SolidBrush(Color.Black));
            Rectangle rt = new Rectangle(5, 5, GraphicsRegion.Width - 10, GraphicsRegion.Height - 10);
            g.DrawRectangle(pen, rt);
            UpdateBitmap();
        }
        private void button5_Click(object sender, EventArgs e)
        {
            //获取textBox中的数值
            int width = int.Parse(textBox2_width.Text);
            int height = int.Parse(textBox3_height.Text);
            //更改pictureBOx5的大小
            pictureBox5.Width = width + 10;
            pictureBox5.Height = height + 10;
            //更新 GraphicRegion数据
            GraphicsRegion.Width = pictureBox5.Width;
            GraphicsRegion.Height = pictureBox5.Height;
            //双缓冲原理
            bitmap = new Bitmap(width + 10, height + 10);   //create a bitmap
            g = Graphics.FromImage(bitmap);    //get the Graphics ref from the bitmap  
            //绘制边框
            Pen pen = new Pen(new SolidBrush(Color.Black));
            Rectangle rt = new Rectangle(5, 5, width, height);
            g.DrawRectangle(pen, rt);
            UpdateBitmap();
            CanStartImproveArrayIndex = true;
            //更新label
            label7.Text = 0.ToString();
            button13_Click(sender, e);
        }

        private void UpdateBitmap()
        {
            pictureBox5.Image = bitmap;
        }
        private void pictureBox5_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            //方便调试的内容输出
            textBox1.Text += "鼠标位置X" + Cursor.Position.X.ToString() + " Y " + Cursor.Position.Y.ToString() + "绘图panel容器X：" + PictureRegion.Left.ToString() + "Y" + PictureRegion.Top.ToString();
            textBox1.Text += "容器width：" + PictureRegion.Width.ToString() + "容器height：" + PictureRegion.Height.ToString();
            textBox1.Text += "滚动条位置scroll X:" + PictureRegion.HorizontalScroll.Value.ToString();
            textBox1.Text += "scroll Y:" + PictureRegion.VerticalScroll.Value.ToString();
            textBox1.Text += "窗口位置X：" + this.Left.ToString() + "Y：" + this.Top.ToString();
            //记录信息的变更
            MouseLocation.X = Cursor.Position.X;
            MouseLocation.Y = Cursor.Position.Y;
            PicBoxLocation.X = PictureRegion.Left;
            PicBoxLocation.Y = PictureRegion.Top;
            PicBoxRegion.Width = PictureRegion.Width;
            PicBoxRegion.Height = PictureRegion.Height;
            WindowsLocation.X = this.Left;
            WindowsLocation.Y = this.Top;
            ScrollLocation.X = PictureRegion.HorizontalScroll.Value;
            ScrollLocation.Y = PictureRegion.VerticalScroll.Value;

            //计算出要绘制的逻辑位置
            GraphicsLocation = CaculateImageLoction(Cursor.Position);
            //输出要绘制的位置信息
            textBox1.Text += "绘制位置：" + GraphicsLocation.X.ToString() + "," + GraphicsLocation.Y.ToString();

            //开始绘制
            GetBitmapFromPicBoxIndex();
            if (CanDraw && CanStartImproveArrayIndex) {

                //用于TreeView1的Index

                g.DrawImage(BitBuffer, GraphicsLocation);
                UpdateBitmap();
                //为已经绘制的arraylist添加元素
                Trans.ImageLocation.X = GraphicsLocation.X;
                Trans.ImageLocation.Y = GraphicsLocation.Y;
                Trans.Index = ImageHasDraw.Count;
                Trans.ChildObjects = new ArrayList();
                ImageHasDraw.Add(Trans);
                //维护label7内容
                label7.Text = ImageHasDraw.Count.ToString();

                //更新索引
                for (int h = 0; h <= ImageHasDraw.Count - 1; h++)
                {
                    DataStructure ds = (DataStructure)ImageHasDraw[h];
                    ds.Index = h;
                }
                //添加到TreeView控件中,  标题的文档初为其 在 ImageHasDraw中的索引值
                TreeNode tn = new TreeNode((ImageHasDraw.Count - 1).ToString() + "," + Trans.ImageName);
                treeView1.Nodes[0].Nodes.Add(tn);
                treeView1.ExpandAll();


            }

        }


        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (pictureBox2.BorderStyle == BorderStyle.None)
            {
                pictureBox2.BorderStyle = BorderStyle.Fixed3D;
            }
            OnlyOnePicBox3d(1);
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            if (pictureBox3.BorderStyle == BorderStyle.None)
            {
                pictureBox3.BorderStyle = BorderStyle.Fixed3D;
            }
            OnlyOnePicBox3d(2);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            if (pictureBox4.BorderStyle == BorderStyle.None)
            {
                pictureBox4.BorderStyle = BorderStyle.Fixed3D;
            }
            OnlyOnePicBox3d(3);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            PictureBox[] pic = { pictureBox1, pictureBox2, pictureBox3, pictureBox4 };
            for (int i = 0; i <= 3; i++)
            {
                if (pic[i].BorderStyle == BorderStyle.Fixed3D)
                {
                    pic[i].BorderStyle = BorderStyle.None;
                }
            }
        }
        //同时只允许一个窗口拥有3d效果
        private void OnlyOnePicBox3d(int index)
        {
            PictureBox[] pic = { pictureBox1, pictureBox2, pictureBox3, pictureBox4 };
            for (int j = 0; j < index; j++)
            {
                pic[j].BorderStyle = BorderStyle.None;
            }
            for (int h = index + 1; h <= 3; h++)
            {
                pic[h].BorderStyle = BorderStyle.None;
            }
        }
        //根据鼠标位置计算出相对的绘制的逻辑位置
        private Point CaculateImageLoction(Point mouseLoaction)
        {
            //鼠标位置-窗口位置=相对位置
            //相对位置-容器与窗口的差值=最终相对坐标
            //画布坐标=最终相对坐标+滚动条位置
            Point relativelocation = new Point();
            Point Lastrelativelocation = new Point();
            Point GraphicsLocation = new Point();

            relativelocation.X = mouseLoaction.X - WindowsLocation.X;
            relativelocation.Y = mouseLoaction.Y - WindowsLocation.Y;

            Lastrelativelocation.X = relativelocation.X - PicBoxLocation.X;
            Lastrelativelocation.Y = relativelocation.Y - PicBoxLocation.Y;
            //451，185  464，220
            GraphicsLocation.X = Lastrelativelocation.X + ScrollLocation.X - 13;
            GraphicsLocation.Y = Lastrelativelocation.Y + ScrollLocation.Y - 30;

            return GraphicsLocation;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (CanStartImproveArrayIndex && ImageHasDraw.Count != 0)
            {
                ClearAll();
                //删掉就近的元素
                ImageHasDraw.RemoveAt(ImageHasDraw.Count - 1);
                //从头到尾重绘
                DrawAll();
                //更新标签
                label7.Text = ImageHasDraw.Count.ToString();
                //更新 TreeView
                UpdateTreeView();
            }
        }
        //更新树结构
        private void UpdateTreeView()
        {
            treeView1.Nodes.Clear();
            //再次创建根节点
            TreeNode root = new TreeNode();
            root.Text = "All Objects";
            treeView1.Nodes.Add(root);

            //此处需要递推进行遍历建立树结构。

            for (int i = 0; i <= ImageHasDraw.Count - 1; i++)
            {
                ScrollTreeView((DataStructure)ImageHasDraw[i], treeView1.Nodes[0]);
            }
            treeView1.ExpandAll();
        }
        //递归函数
        private void ScrollTreeView(DataStructure TheObject, TreeNode ParentNode)
        {
            //保存当前的TreeNode
            string[] str = ParentNode.Text.Split(',');
            string str1 = str[0].ToString() + ",";
            if (ParentNode == treeView1.Nodes[0])
            {
                str1 = "";
            }
            TreeNode next = new TreeNode(str1 + TheObject.Index.ToString() + "," + TheObject.ImageName);
            ParentNode.Nodes.Add(next);

            //保存子TreeNode；
            if (TheObject.ChildObjects != null)
            {
                for (int i = 0; i <= TheObject.ChildObjects.Count - 1; i++)
                {
                    //更新Index
                    DataStructure ds1 = (DataStructure)TheObject.ChildObjects[i];
                    ds1.Index = i;
                    ScrollTreeView(ds1, next);
                }
            }

        }
        //全部重新绘制的函数
        private void DrawAll()
        {
            for (int i = 0; i <= ImageHasDraw.Count - 1; i++)
            {
                g.DrawImage(((DataStructure)ImageHasDraw[i]).Image, ((DataStructure)ImageHasDraw[i]).ImageLocation);
                if (((DataStructure)ImageHasDraw[i]).ChildObjects != null)
                {
                    ArrayList ar = ((DataStructure)ImageHasDraw[i]).ChildObjects;
                    for (int j = 0; j <= ar.Count - 1; j++)
                    {
                        g.DrawImage(((DataStructure)ar[j]).Image, ((DataStructure)ar[j]).ImageLocation);
                    }
                }
            }
            UpdateBitmap();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (CanStartImproveArrayIndex)
            {
                ClearAll();
                ImageHasDraw = new ArrayList();
                //更新标签
                label7.Text = 0.ToString();

                //更新TreeView
                treeView1.Nodes.Clear();
                //再次创建根节点
                TreeNode root = new TreeNode();
                root.Text = "All Objects";
                treeView1.Nodes.Add(root);

            }
        }
        private int GetIndexFromOkn(int x)
        {
            int Index = (NowPage - 1) * 4 + x;
            return Index;
        }
        private void ok2_Click(object sender, EventArgs e)
        {
            int index = GetIndexFromOkn(1);
            if ((pictureBox2.Image != null) && CanStartImproveArrayIndex)
            {
                int x = int.Parse(textBox4.Text);
                int y = int.Parse(textBox3.Text);
                //此处直接更改文件大小，然后，重新导入目标位置文件。
                //1
                DataStructure ds1 = ((DataStructure)Data[index]);
                Bitmap bt1 = ds1.Image;
                Bitmap bt2 = new Bitmap(bt1, x, y);
                ds1.Image = bt2;
                Data[index] = ds1;
                //重新导入目标位置文件
                pictureBox2.Image = ds1.Image;
                //
                textBox9.Text = index.ToString();
            }
        }

        private void ok1_Click(object sender, EventArgs e)
        {
            int index = GetIndexFromOkn(0);
            if ((pictureBox1.Image != null) && CanStartImproveArrayIndex)
            {
                int x = int.Parse(textBox_1.Text);
                int y = int.Parse(textBox2.Text);
                //此处直接更改文件大小，然后，重新导入目标位置文件。
                //1
                DataStructure ds1 = ((DataStructure)Data[index]);
                Bitmap bt1 = ds1.Image;
                Bitmap bt2 = new Bitmap(bt1, x, y);
                ds1.Image = bt2;
                Data[index] = ds1;
                //重新导入目标位置文件
                pictureBox1.Image = ds1.Image;
                //
                textBox9.Text = index.ToString();

            }
        }
        //一些行为的封装--Ok按钮对应的更新 ImageHasDraw 与label7
        private void UpImgAndLabelBecuseOfOk(int x, int y)
        {
            UpdateBitmap();
            //为已经绘制的arraylist添加元素
            Trans.ImageLocation.X = x;
            Trans.ImageLocation.Y = y;
            ImageHasDraw.Add(Trans);
            //维护label7内容
            label7.Text = ImageHasDraw.Count.ToString();
        }

        private void ok3_Click(object sender, EventArgs e)
        {
            int index = GetIndexFromOkn(2);
            if ((pictureBox3.Image != null) && CanStartImproveArrayIndex)
            {
                int x = int.Parse(textBox6.Text);
                int y = int.Parse(textBox5.Text);
                //此处直接更改文件大小，然后，重新导入目标位置文件。
                //1
                DataStructure ds1 = ((DataStructure)Data[index]);
                Bitmap bt1 = ds1.Image;
                Bitmap bt2 = new Bitmap(bt1, x, y);
                ds1.Image = bt2;
                Data[index] = ds1;
                //重新导入目标位置文件
                pictureBox3.Image = ds1.Image;
            }
        }

        private void ok4_Click(object sender, EventArgs e)
        {
            int index = GetIndexFromOkn(3);
            if ((pictureBox4.Image != null) && CanStartImproveArrayIndex)
            {
                int x = int.Parse(textBox8.Text);
                int y = int.Parse(textBox7.Text);
                //此处直接更改文件大小，然后，重新导入目标位置文件。
                //1
                DataStructure ds1 = ((DataStructure)Data[index]);
                Bitmap bt1 = ds1.Image;
                Bitmap bt2 = new Bitmap(bt1, x, y);
                ds1.Image = bt2;
                Data[index] = ds1;
                //重新导入目标位置文件
                pictureBox4.Image = ds1.Image;
            }
        }

        private void 作者ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Code By YanXin,2016,8", "作者");

        }

        private void 版本ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Version 1.0", "版本");
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            textBox9.Text = "";
            //获取对象，并且合并
            /*****************************
            1，建立临时数据结构，存储索引值，显示在textBOX中
            2，建立临时数据结构，存储获取的对象
            3，合并操作。记住是值传递
            *****************************/
            if (treeView1.SelectedNode != treeView1.Nodes[0])
            {
                String text = treeView1.SelectedNode.Text;
                String[] indexs = text.Split(',');
                IndexForMerge.Add(indexs[0]);

                //防止重复
                int Count = IndexForMerge.Count;
                for (int i = 0; i <= Count - 1; i++)
                {
                    if (int.Parse(indexs[0].ToString()) == int.Parse((string)(IndexForMerge[i].ToString())) && i != Count - 1)
                    {
                        IndexForMerge.RemoveAt(IndexForMerge.Count - 1);
                        break;
                    }
                }

                for (int i = 0; i <= IndexForMerge.Count - 1; i++)
                {
                    textBox9.Text += ((string)(IndexForMerge[i].ToString())) + ",";
                }
                //获取其坐标
                GetLocationOfSelectedNode();

            }


        }

        private void button10_Click(object sender, EventArgs e)
        {
            IndexForMerge.Clear();
            textBox9.Text = "";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                //1，获取选中的节点的DataStructure
                //2，判断是否包含子节点。 包含--》拆分，更新imageHasDraw，更新索引，更新树结构
                string[] str = treeView1.SelectedNode.Text.Split(',');
                string str1 = str[0].ToString();
                int index = int.Parse(str1);
                if (((DataStructure)ImageHasDraw[index]).ChildObjects != null)
                {
                    int number = ((DataStructure)ImageHasDraw[index]).ChildObjects.Count;
                    ArrayList ar = ((DataStructure)ImageHasDraw[index]).ChildObjects;
                    for (int i = 0; i <= number - 1; i++)
                    {
                        ImageHasDraw.Add(ar[i]);

                    }
                    //清空子节点
                    ar.Clear();
                    for (int j = 0; j <= ImageHasDraw.Count - 1; j++)
                    {
                        DataStructure ds = (DataStructure)ImageHasDraw[j];
                        ds.Index = j;
                        ImageHasDraw[j] = ds;                //值传递陷阱。 但是为什么偏偏在此地发生。此处获取的是整个ds对象。
                        UpdateImageHasDrawIndex(ds);
                    }

                    //更新TreeView1
                    UpdateTreeView();
                }
            }
            catch (System.NullReferenceException ex)
            {
                MessageBox.Show("TreeView中无可用对象");
            }

        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            try
            {
                button3_Click(sender, e);
                //获取选中Node的DataStructure
                //更改其ImageLocation以及子ImageLocation属性
                //更新ImageHasDraw结构
                int Unit = int.Parse(textBox12.Text);
                TreeNode tn = treeView1.SelectedNode;
                string[] str = treeView1.SelectedNode.Text.Split(',');
                string str1 = str[0].ToString();
                string str2 = str[1].ToString();
                int index1 = int.Parse(str1);
                int index2;
                DataStructure ds;
                DataStructure dsChild;
                try
                {
                    index2 = int.Parse(str2);
                }
                catch (Exception ex)
                {
                    //说明为非子对象
                    ds = (DataStructure)ImageHasDraw[index1];
                    ds.ImageLocation.Y -= Unit;
                    //使得TreeView1中的相关对象仍然为选中状态
                    //更新其所有子元素的 坐标
                    if (ds.ChildObjects != null)
                    {
                        for (int i = 0; i <= ds.ChildObjects.Count - 1; i++)
                        {
                            dsChild = ((DataStructure)ds.ChildObjects[i]);
                            dsChild.ImageLocation.Y -= Unit;
                            ds.ChildObjects[i] = dsChild;
                        }
                    }
                    ImageHasDraw[index1] = ds;
                    treeView1.SelectedNode = tn;

                    textBox10.Text = ds.ImageLocation.X.ToString();
                    textBox11.Text = ds.ImageLocation.Y.ToString();

                    ClearAll();
                    DrawAll();
                    return;
                }
                //为子对象
                ds = (DataStructure)ImageHasDraw[index1];
                dsChild = (DataStructure)(ds.ChildObjects[index2]);
                dsChild.ImageLocation.Y -= Unit;
                ds.ChildObjects[index2] = dsChild;
                ImageHasDraw[index1] = ds;
                treeView1.SelectedNode = tn;

                textBox10.Text = ds.ImageLocation.X.ToString();
                textBox11.Text = ds.ImageLocation.Y.ToString();
                ClearAll();
                DrawAll();

            }
            catch (System.NullReferenceException ex)
            {

            }

        }

        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                button3_Click(sender, e);
                //获取选中Node的DataStructure
                //更改其ImageLocation以及子ImageLocation属性
                //更新ImageHasDraw结构
                int Unit = int.Parse(textBox12.Text);
                TreeNode tn = treeView1.SelectedNode;
                string[] str = treeView1.SelectedNode.Text.Split(',');
                string str1 = str[0].ToString();
                string str2 = str[1].ToString();
                int index1 = int.Parse(str1);
                int index2;
                DataStructure ds;
                DataStructure dsChild;
                try
                {
                    index2 = int.Parse(str2);
                }
                catch (Exception ex)
                {
                    //说明为非子对象
                    ds = (DataStructure)ImageHasDraw[index1];
                    ds.ImageLocation.X -= Unit;
                    //使得TreeView1中的相关对象仍然为选中状态
                    //更新其所有子元素的 坐标
                    if (ds.ChildObjects != null)
                    {
                        for (int i = 0; i <= ds.ChildObjects.Count - 1; i++)
                        {
                            dsChild = ((DataStructure)ds.ChildObjects[i]);
                            dsChild.ImageLocation.X -= Unit;
                            ds.ChildObjects[i] = dsChild;
                        }
                    }
                    ImageHasDraw[index1] = ds;
                    treeView1.SelectedNode = tn;

                    textBox10.Text = ds.ImageLocation.X.ToString();
                    textBox11.Text = ds.ImageLocation.Y.ToString();

                    ClearAll();
                    DrawAll();
                    return;
                }
                //为子对象
                ds = (DataStructure)ImageHasDraw[index1];
                dsChild = (DataStructure)(ds.ChildObjects[index2]);
                dsChild.ImageLocation.X -= Unit;
                ds.ChildObjects[index2] = dsChild;
                ImageHasDraw[index1] = ds;
                treeView1.SelectedNode = tn;

                textBox10.Text = ds.ImageLocation.X.ToString();
                textBox11.Text = ds.ImageLocation.Y.ToString();
                ClearAll();
                DrawAll();

            }
            catch (System.NullReferenceException ex)
            {

            }

        }

        private void button14_Click(object sender, EventArgs e)
        {
            try
            {
                button3_Click(sender, e);
                //获取选中Node的DataStructure
                //更改其ImageLocation以及子ImageLocation属性
                //更新ImageHasDraw结构
                int Unit = int.Parse(textBox12.Text);
                TreeNode tn = treeView1.SelectedNode;
                string[] str = treeView1.SelectedNode.Text.Split(',');
                string str1 = str[0].ToString();
                string str2 = str[1].ToString();
                int index1 = int.Parse(str1);
                int index2;
                DataStructure ds;
                DataStructure dsChild;
                try
                {
                    index2 = int.Parse(str2);
                }
                catch (Exception ex)
                {
                    //说明为非子对象
                    ds = (DataStructure)ImageHasDraw[index1];
                    ds.ImageLocation.Y += Unit;
                    //使得TreeView1中的相关对象仍然为选中状态
                    //更新其所有子元素的 坐标
                    if (ds.ChildObjects != null)
                    {
                        for (int i = 0; i <= ds.ChildObjects.Count - 1; i++)
                        {
                            dsChild = ((DataStructure)ds.ChildObjects[i]);
                            dsChild.ImageLocation.Y += Unit;
                            ds.ChildObjects[i] = dsChild;
                        }
                    }
                    ImageHasDraw[index1] = ds;
                    treeView1.SelectedNode = tn;

                    textBox10.Text = ds.ImageLocation.X.ToString();
                    textBox11.Text = ds.ImageLocation.Y.ToString();

                    ClearAll();
                    DrawAll();
                    return;
                }
                //为子对象
                ds = (DataStructure)ImageHasDraw[index1];
                dsChild = (DataStructure)(ds.ChildObjects[index2]);
                dsChild.ImageLocation.Y += Unit;
                ds.ChildObjects[index2] = dsChild;
                ImageHasDraw[index1] = ds;
                treeView1.SelectedNode = tn;

                textBox10.Text = ds.ImageLocation.X.ToString();
                textBox11.Text = ds.ImageLocation.Y.ToString();
                ClearAll();
                DrawAll();

            }
            catch (System.NullReferenceException ex)
            {

            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            try
            {
                button3_Click(sender, e);
                //获取选中Node的DataStructure
                //更改其ImageLocation以及子ImageLocation属性
                //更新ImageHasDraw结构
                int Unit = int.Parse(textBox12.Text);
                TreeNode tn = treeView1.SelectedNode;
                string[] str = treeView1.SelectedNode.Text.Split(',');
                string str1 = str[0].ToString();
                string str2 = str[1].ToString();
                int index1 = int.Parse(str1);
                int index2;
                DataStructure ds;
                DataStructure dsChild;
                try
                {
                    index2 = int.Parse(str2);
                }
                catch (Exception ex)
                {
                    //说明为非子对象
                    ds = (DataStructure)ImageHasDraw[index1];
                    ds.ImageLocation.X += Unit;
                    //使得TreeView1中的相关对象仍然为选中状态
                    //更新其所有子元素的 坐标
                    if (ds.ChildObjects != null)
                    {
                        for (int i = 0; i <= ds.ChildObjects.Count - 1; i++)
                        {
                            dsChild = ((DataStructure)ds.ChildObjects[i]);
                            dsChild.ImageLocation.X += Unit;
                            ds.ChildObjects[i] = dsChild;
                        }
                    }
                    ImageHasDraw[index1] = ds;
                    treeView1.SelectedNode = tn;

                    textBox10.Text = ds.ImageLocation.X.ToString();
                    textBox11.Text = ds.ImageLocation.Y.ToString();
                    ClearAll();
                    DrawAll();
                    return;
                }
                //为子对象
                ds = (DataStructure)ImageHasDraw[index1];
                dsChild = (DataStructure)(ds.ChildObjects[index2]);
                dsChild.ImageLocation.X += Unit;
                ds.ChildObjects[index2] = dsChild;
                ImageHasDraw[index1] = ds;
                treeView1.SelectedNode = tn;

                textBox10.Text = ds.ImageLocation.X.ToString();
                textBox11.Text = ds.ImageLocation.Y.ToString();
                ClearAll();
                DrawAll();
            }
            catch (System.NullReferenceException ex)
            {

            }

        }

        private void button15_Click(object sender, EventArgs e)
        {
            try
            {
                //获取textBox中数值
                int Lx = int.Parse(textBox10.Text);
                int Ly = int.Parse(textBox11.Text);
                button3_Click(sender, e);
                //获取选中Node的DataStructure
                //更改其ImageLocation以及子ImageLocation属性
                //更新ImageHasDraw结构
                int Unit = int.Parse(textBox12.Text);
                TreeNode tn = treeView1.SelectedNode;
                string[] str = treeView1.SelectedNode.Text.Split(',');
                string str1 = str[0].ToString();
                string str2 = str[1].ToString();
                int index1 = int.Parse(str1);
                int index2;
                DataStructure ds;
                DataStructure dsChild;
                try
                {
                    index2 = int.Parse(str2);
                }
                catch (Exception ex)
                {
                    //说明为非子对象
                    ds = (DataStructure)ImageHasDraw[index1];
                    int LxRe = Lx - ds.ImageLocation.X;
                    int LyRe = Ly - ds.ImageLocation.Y;
                    ds.ImageLocation.X = Lx;
                    ds.ImageLocation.Y = Ly;
                    //使得TreeView1中的相关对象仍然为选中状态
                    //更新其所有子元素的 坐标
                    if (ds.ChildObjects != null)
                    {
                        for (int i = 0; i <= ds.ChildObjects.Count - 1; i++)
                        {
                            dsChild = ((DataStructure)ds.ChildObjects[i]);
                            dsChild.ImageLocation.X = dsChild.ImageLocation.X + LxRe;
                            dsChild.ImageLocation.Y = dsChild.ImageLocation.Y + LyRe;
                            ds.ChildObjects[i] = dsChild;
                        }
                    }
                    ImageHasDraw[index1] = ds;
                    treeView1.SelectedNode = tn;

                    textBox10.Text = ds.ImageLocation.X.ToString();
                    textBox11.Text = ds.ImageLocation.Y.ToString();
                    ClearAll();
                    DrawAll();
                    return;
                }
                //为子对象
                ds = (DataStructure)ImageHasDraw[index1];
                dsChild = (DataStructure)(ds.ChildObjects[index2]);
                dsChild.ImageLocation.X = Lx;
                dsChild.ImageLocation.Y = Ly;
                ds.ChildObjects[index2] = dsChild;
                ImageHasDraw[index1] = ds;
                treeView1.SelectedNode = tn;

                textBox10.Text = ds.ImageLocation.X.ToString();
                textBox11.Text = ds.ImageLocation.Y.ToString();
                ClearAll();
                DrawAll();
            }
            catch (System.NullReferenceException ex)
            {

            }
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {

        }
        //由被选中的节点获取其坐标
        private void GetLocationOfSelectedNode()
        {
            //获取textBox中数值

            //获取选中Node的DataStructure
            //更改其ImageLocation以及子ImageLocation属性
            //更新ImageHasDraw结构
            int Unit = int.Parse(textBox12.Text);
            TreeNode tn = treeView1.SelectedNode;
            string[] str = treeView1.SelectedNode.Text.Split(',');
            string str1 = str[0].ToString();
            string str2 = str[1].ToString();
            int index1 = int.Parse(str1);
            int index2;
            DataStructure ds;
            DataStructure dsChild;
            try
            {
                index2 = int.Parse(str2);
            }
            catch (Exception ex)
            {
                //说明为非子对象
                ds = (DataStructure)ImageHasDraw[index1];
                textBox10.Text = ds.ImageLocation.X.ToString();
                textBox11.Text = ds.ImageLocation.Y.ToString();
                //使得TreeView1中的相关对象仍然为选中状态

                ImageHasDraw[index1] = ds;
                treeView1.SelectedNode = tn;
                return;
            }
            //为子对象
            ds = (DataStructure)ImageHasDraw[index1];
            dsChild = (DataStructure)(ds.ChildObjects[index2]);
            treeView1.SelectedNode = tn;
            textBox10.Text = dsChild.ImageLocation.X.ToString();
            textBox11.Text = dsChild.ImageLocation.Y.ToString();

        }

        private void 添加碰撞体对象ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //创建一个bitmap，获取其Graphics，根据默认大小绘制矩形
            //2,调用 导入 方法中的操作， 传入到列表中，设置属性 isColider=true
            //1,
            Bitmap bt = new Bitmap(100, 100);
            Graphics gp1 = Graphics.FromImage(bt);
            Rectangle rt1 = new Rectangle(100, 100, 100, 100);
            gp1.DrawRectangle(new Pen(new SolidBrush(Color.Blue)), rt1);

            //2
            DataStructure ds = new DataStructure();
            ds.Image = bt;
            ds.ImageLocation = new Point(0, 0);
            ds.Index = 0;
            ds.isColider = true;
            ds.ImageName = "Colider";

            Data.Add(ds);
            textBox1.Text = Data.Count.ToString();
            //只要最后一位的bitmap
            Bitmap bt1 = ((DataStructure)Data[Data.Count - 1]).Image;
            Bitmap bt2 = getBitMap(bt1);  //转换大小
            SetPicBoxPicture(ref pictureBox1, ref pictureBox2, ref pictureBox3, ref pictureBox4, bt2);   //图片从data--》picBOX
            UpdatePage(Data);   //更新页数
            UpdateDataCount(Data);  //更新List总数目
        }

        private void 导入ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog op1 = new OpenFileDialog();
                op1.Multiselect = true;
                op1.Filter = "PNG文件(*.png)|*.PNG|JPG文件(*.jpg)|*.jpg";
                string[] filenames;        //保存文件名的ArrayList
                if (op1.ShowDialog() == DialogResult.OK)
                {
                    filenames = op1.FileNames;
                }
                else
                {
                    filenames = null;
                }
                for (int i = 0; i <= filenames.Length - 1; i++)
                {
                    //将结构体补充完整，然后添加到Data中
                    DataStructure a;
                    a.Image = new Bitmap(filenames[i]);
                    a.ImageName = filenames[i];
                    a.Index = 0;
                    Point b = new Point(0, 0);
                    a.ImageLocation = b;
                    a.isColider = false;
                    a.ChildObjects = null;
                    //add the picture in the Data
                    Data.Add(a);
                    textBox1.Text = Data.Count.ToString();
                    //只要最后一位的bitmap

                    Bitmap bt = ((DataStructure)Data[Data.Count - 1]).Image;
                    Bitmap bt1 = getBitMap(bt);  //转换大小
                    SetPicBoxPicture(ref pictureBox1, ref pictureBox2, ref pictureBox3, ref pictureBox4, bt1);   //图片从data--》picBOX
                    UpdatePage(Data);   //更新页数
                    UpdateDataCount(Data);  //更新List总数目
                }

                }catch (System.NullReferenceException ex)
            {

            }

        }
        private void 作者ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Code By YanXin,2016,8", "作者");
        }

        private void 版本ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Version 1.0", "版本");
        }

        private void 添加碰撞体ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //创建一个bitmap，获取其Graphics，根据默认大小绘制矩形
            //2,调用 导入 方法中的操作， 传入到列表中，设置属性 isColider=true
            //1,
            Bitmap bt = new Bitmap(100, 100);
            Graphics gp1 = Graphics.FromImage(bt);
            Rectangle rt1 = new Rectangle(0, 0, 95, 95);
            gp1.DrawRectangle(new Pen(new SolidBrush(Color.Blue)), rt1);

            //2
            DataStructure ds = new DataStructure();
            ds.Image = bt;
            ds.ImageLocation = new Point(0, 0);
            ds.Index = 0;
            ds.isColider = true;
            ds.ImageName = "Colider";
            ds.ChildObjects = null;

            Data.Add(ds);
            textBox1.Text = Data.Count.ToString();
            //只要最后一位的bitmap
            Bitmap bt1 = ((DataStructure)Data[Data.Count - 1]).Image;
            Bitmap bt2 = getBitMap(bt1);  //转换大小
            SetPicBoxPicture(ref pictureBox1, ref pictureBox2, ref pictureBox3, ref pictureBox4, bt2);   //图片从data--》picBOX
            UpdatePage(Data);   //更新页数
            UpdateDataCount(Data);  //更新List总数目
        }

        private void 保存到数据库ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            /**********************************
            遍历读取ImageHasDraw结构，第一层
            写入数据库
            遍历第二层，写入数据库。
            *************************************/
            DBConnect db = new DBConnect();
            db.Initialize("localhost", "hunter", "root", "111", "3306");
            string strForInsert = "INSERT INTO hunter_1 (hunter_index, hunter_imagefilename, hunter_iscolider,location_X,location_Y,lengthx,lengthy) VALUES";
            for(int i = 0; i <= ImageHasDraw.Count - 1; i++)
            {
                DataStructure ds = (DataStructure)ImageHasDraw[i];
                string index ="'"+ ds.Index.ToString()+"'";
                //
                string imagefilename = System.IO.Path.GetFileName(ds.ImageName);
                imagefilename = "'"+imagefilename+"'";
                //
                string location_X = "'" + ds.ImageLocation.X.ToString()+"'";
                string location_Y = "'" + ds.ImageLocation.Y.ToString() + "'";
                string iscolider;
                string lengthx = "'" + ds.Image.Width+ "'";
                string lengthy = "'" + ds.Image.Height+ "'";

                if (ds.isColider == false)
                {
                    iscolider = "'" + 0.ToString() + "'";
                }
                else
                {
                    iscolider = "'" + 1.ToString() + "'";
                }
                string split = ",";
                //string fatherindex = "'"+ "NULL"+ "'";
                //string childindex = "'"+"NULL"+"'";
                string ReadyToInset = strForInsert + "(" + index + split + imagefilename + split + iscolider + split + location_X + split + location_Y +split+lengthx+split+lengthy +")";
                db.Insert(ReadyToInset);
            }
            //遍历第二层
            strForInsert = "INSERT INTO hunter_1 (hunter_index, hunter_imagefilename, hunter_iscolider,hunter_fatherindex,hunter_childindex,location_X,location_Y,lengthx,lengthy) VALUES";
            int IndexForAdd = 0;
            for (int i = 0; i <= ImageHasDraw.Count - 1; i++)
            {
                DataStructure ds1 = (DataStructure)ImageHasDraw[i];
                if (ds1.ChildObjects != null)
                {
                    for(int j = 0; j <= ds1.ChildObjects.Count - 1; j++)
                    {
                        IndexForAdd++;
                        DataStructure ds2 = (DataStructure)ds1.ChildObjects[j];
                        string index = "'" +( ImageHasDraw.Count-1+IndexForAdd).ToString() + "'";
                        //
                        string imagefilename = System.IO.Path.GetFileName(ds2.ImageName);
                        imagefilename = "'" + imagefilename + "'";
                        //
                        string location_X = "'" + ds2.ImageLocation.X.ToString() + "'";
                        string location_Y = "'" + ds2.ImageLocation.Y.ToString() + "'";
                        string lengthx = "'" + ds2.Image.Width + "'";
                        string lengthy = "'" + ds2.Image.Height + "'";
                        string iscolider;
                        if (ds2.isColider == false)
                        {
                            iscolider = "'" + 0.ToString() + "'";
                        }
                        else
                        {
                            iscolider = "'" + 1.ToString() + "'";
                        }
                        string split = ",";
                        string fatherindex = "'"+ i.ToString()+ "'";
                        string childindex = "'"+j.ToString()+"'";
                        string ReadyToInset = strForInsert + "(" + index + split + imagefilename + split + iscolider + split + fatherindex +split+childindex+split+location_X + split + location_Y + split+lengthx+split+lengthy+")";
                        db.Insert(ReadyToInset);
                    }
                }
            }
            MessageBox.Show("插入数据库成功");
     }
     private string GetFileNameByFullPath(string FullPath)
        {
            string filename = System.IO.Path.GetFileName(FullPath);//文件名  “Default.aspx”
            // string extension = System.IO.Path.GetExtension(fullPath);//扩展名 “.aspx”
            //string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fullPath);// 没有扩展名的文件名 “Default”
            return filename;
        }
    }
}
