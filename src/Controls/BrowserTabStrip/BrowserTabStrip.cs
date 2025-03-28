using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;

namespace SharpBrowser.Controls.BrowserTabStrip {
	[DefaultEvent("TabStripItemSelectionChanged")]
	[DefaultProperty("Items")]
	[ToolboxItem(true)]
	public class BrowserTabStrip : BaseStyledPanel, ISupportInitialize, IDisposable {
		
		private const int TEXT_LEFT_MARGIN = 15;
		private const int TEXT_RIGHT_MARGIN = 10;
		private const int DEF_HEADER_HEIGHT = 28;
        private const int DEF_GLYPH_WIDTH = 40;

        //private const int DEF_BUTTON_HEIGHT = 28;
        public int TabButton_Height => DEF_BUTTON_HEIGHT;
        private const int DEF_BUTTON_HEIGHT = 48;
		private int DEF_START_POS = 50;
		private int DEF_START_POS_forOnPaint = 50; //volatile

        private Rectangle stripButtonRect = Rectangle.Empty;

		private BrowserTabStripItem selectedItem;
		private ContextMenuStrip menu;
		private BrowserTabStripCloseButton closeButton;
		private BrowserTabStripItemCollection items;

		private StringFormat sf;
		private static Font defaultFont = new Font("Tahoma", 8.25f, FontStyle.Regular);

        private bool isIniting;
		private bool menuOpen;
		public int MaxTabSize = 200;
		public int AddButtonWidth = 40;

        [RefreshProperties(RefreshProperties.All)]
		[DefaultValue(null)]
		public BrowserTabStripItem SelectedItem {
			get {
				return selectedItem;
			}
			set {
				if (selectedItem == value) {
					return;
				}
				if (value == null && Items.Count > 0) {
					BrowserTabStripItem fATabStripItem = Items[0];
					if (fATabStripItem.Visible) {
						selectedItem = fATabStripItem;
						selectedItem.Selected = true;
						selectedItem.Dock = DockStyle.Fill;
					}
				}
				else {
					selectedItem = value;
				}

				foreach (BrowserTabStripItem item in Items) {
					if (item == selectedItem) {
						SelectItem(item);
						item.Dock = DockStyle.Fill;
						item.Show();
					}
					else {
						UnSelectItem(item);
						item.Hide();
					}
				}
				SelectItem(selectedItem);
				Invalidate();
				if (!selectedItem.IsDrawn) {
					Items.MoveTo(0, selectedItem);
					Invalidate();
				}
				OnTabStripItemChanged(new TabStripItemChangedEventArgs(selectedItem, BrowserTabStripItemChangeTypes.SelectionChanged));
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public BrowserTabStripItemCollection Items => items;

		[DefaultValue(typeof(Size), "350,200")]
		public new Size Size {
			get {
				return base.Size;
			}
			set {
				if (!(base.Size == value)) {
					base.Size = value;
					UpdateLayout();
				}
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new ControlCollection Controls => base.Controls;

		public event TabStripItemClosingHandler TabStripItemClosing;
		public event TabStripItemChangedHandler TabStripItemSelectionChanged;
		public event HandledEventHandler MenuItemsLoading;
		public event EventHandler MenuItemsLoaded;
		public event EventHandler TabStripItemClosed;

		public BrowserTabStrip() {
			BeginInit();
			SetStyle(ControlStyles.ContainerControl, value: true);
			SetStyle(ControlStyles.UserPaint, value: true);
			SetStyle(ControlStyles.ResizeRedraw, value: true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
			SetStyle(ControlStyles.Selectable, value: true);
			items = new BrowserTabStripItemCollection();
			items.CollectionChanged += OnCollectionChanged;
			base.Size = new Size(350, 200);
			menu = new ContextMenuStrip();
			menu.Renderer = base.ToolStripRenderer;
			menu.ItemClicked += OnMenuItemClicked;
			menu.VisibleChanged += OnMenuVisibleChanged;
			closeButton = new BrowserTabStripCloseButton(base.ToolStripRenderer);
			Font = defaultFont;
			sf = new StringFormat();
			EndInit();
			UpdateLayout();
		}

		public HitTestResult HitTest(Point pt) {
			if (closeButton.IsVisible && closeButton.Rect.Contains(pt)) {
				return HitTestResult.CloseButton;
			}
			if (GetTabItemByPoint(pt) != null) {
				return HitTestResult.TabItem;
			}
			return HitTestResult.None;
		}

        public void AddTab(BrowserTabStripItem tabItem) => AddTab(tabItem, autoSelect: false);

        public void AddTab(BrowserTabStripItem tabItem, bool autoSelect) {
			tabItem.Dock = DockStyle.Fill;
			Items.Add(tabItem);
			if ((autoSelect && tabItem.Visible) || (tabItem.Visible && Items.DrawnCount < 1)) {
				SelectedItem = tabItem;
				SelectItem(tabItem);
			}
		}

		public void RemoveTab(BrowserTabStripItem tabItem) {
			int num = Items.IndexOf(tabItem);
			if (num >= 0) {
				UnSelectItem(tabItem);
				Items.Remove(tabItem);
			}
			if (Items.Count > 0) {
				if (Items[num - 1] != null) {
					SelectedItem = Items[num - 1];
				}
				else {
					SelectedItem = Items.FirstVisible;
				}
			}
		}

		public BrowserTabStripItem GetTabItemByPoint(Point pt) {
			BrowserTabStripItem result = null;
			bool flag = false;
			for (int i = 0; i < Items.Count; i++) {
				BrowserTabStripItem fATabStripItem = Items[i];
				if (fATabStripItem.StripRect.Contains(pt) && fATabStripItem.Visible && fATabStripItem.IsDrawn) {
					result = fATabStripItem;
					flag = true;
				}
				if (flag) {
					break;
				}
			}
			return result;
		}

		public virtual void ShowMenu() { }

		internal void UnDrawAll() {
			for (int i = 0; i < Items.Count; i++) {
				Items[i].IsDrawn = false;
			}
		}

		internal void SelectItem(BrowserTabStripItem tabItem) {
			tabItem.Dock = DockStyle.Fill;
			tabItem.Visible = true;
			tabItem.Selected = true;
		}

        internal void UnSelectItem(BrowserTabStripItem tabItem) => tabItem.Selected = false;

        protected internal virtual void OnTabStripItemClosing(TabStripItemClosingEventArgs e) => this.TabStripItemClosing?.Invoke(e);

        protected internal virtual void OnTabStripItemClosed(EventArgs e) {
			selectedItem = null;
            this.TabStripItemClosed?.Invoke(this, e);
        }

        protected virtual void OnMenuItemsLoading(HandledEventArgs e) => this.MenuItemsLoading?.Invoke(this, e);

        protected virtual void OnMenuItemsLoaded(EventArgs e) {
			if (this.MenuItemsLoaded != null) {
				this.MenuItemsLoaded(this, e);
			}
		}

		protected virtual void OnTabStripItemChanged(TabStripItemChangedEventArgs e) {
            this.TabStripItemSelectionChanged?.Invoke(e);
        }

        protected virtual void OnMenuItemsLoad(EventArgs e) {
			menu.RightToLeft = RightToLeft;
			menu.Items.Clear();
			for (int i = 0; i < Items.Count; i++) {
				BrowserTabStripItem fATabStripItem = Items[i];
				if (fATabStripItem.Visible) {
					ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(fATabStripItem.Title);
					toolStripMenuItem.Tag = fATabStripItem;
					toolStripMenuItem.Image = fATabStripItem.Image;
					menu.Items.Add(toolStripMenuItem);
				}
			}
			OnMenuItemsLoaded(EventArgs.Empty);
		}

		protected override void OnRightToLeftChanged(EventArgs e) {
			base.OnRightToLeftChanged(e);
			UpdateLayout();
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e) {
			base.OnMouseDown(e);
			HitTestResult hitTestResult = HitTest(e.Location);
			if (hitTestResult == HitTestResult.TabItem) {
				BrowserTabStripItem tabItemByPoint = GetTabItemByPoint(e.Location);
				if (tabItemByPoint != null) {
					SelectedItem = tabItemByPoint;
					Invalidate();
				}

                //close if wheel click (aka middle)
                if (e.Button == MouseButtons.Middle )
                {
                    //close action??
                    if (SelectedItem != null)
                    {
                        TabStripItemClosingEventArgs tabStripItemClosingEventArgs = new TabStripItemClosingEventArgs(SelectedItem);
                        OnTabStripItemClosing(tabStripItemClosingEventArgs);
                        if (!tabStripItemClosingEventArgs.Cancel && SelectedItem.CanClose)
                        {
                            RemoveTab(SelectedItem);
                            OnTabStripItemClosed(EventArgs.Empty);
                        }
                    }
                    Invalidate();

                }
            }
			else {
				if (e.Button != MouseButtons.Left || hitTestResult != 0) {
					return;
				}
				if (SelectedItem != null) {
					TabStripItemClosingEventArgs tabStripItemClosingEventArgs = new TabStripItemClosingEventArgs(SelectedItem);
					OnTabStripItemClosing(tabStripItemClosingEventArgs);
					if (!tabStripItemClosingEventArgs.Cancel && SelectedItem.CanClose) {
						RemoveTab(SelectedItem);
						OnTabStripItemClosed(EventArgs.Empty);
					}
				}
				Invalidate();
			}
		}
		
		protected override void OnMouseMove(MouseEventArgs e) {
			base.OnMouseMove(e);
			if (closeButton.IsVisible) {
				if (closeButton.Rect.Contains(e.Location)) {
					closeButton.IsMouseOver = true;
					Invalidate(closeButton.RedrawRect);
				}
				else if (closeButton.IsMouseOver) {
					closeButton.IsMouseOver = false;
					Invalidate(closeButton.RedrawRect);
				}
			}
		}

		protected override void OnMouseLeave(EventArgs e) {
			base.OnMouseLeave(e);
			closeButton.IsMouseOver = false;
			if (closeButton.IsVisible) {
				Invalidate(closeButton.RedrawRect);
			}
		}

		protected override void OnSizeChanged(EventArgs e) {
			base.OnSizeChanged(e);
			if (!isIniting) {
				UpdateLayout();
			}
		}

		private void SetDefaultSelected() {
			if (selectedItem == null && Items.Count > 0) {
				SelectedItem = Items[0];
			}
			for (int i = 0; i < Items.Count; i++) {
				BrowserTabStripItem fATabStripItem = Items[i];
				fATabStripItem.Dock = DockStyle.Fill;
			}
		}

		private void OnMenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
			BrowserTabStripItem fATabStripItem2 = (SelectedItem = (BrowserTabStripItem)e.ClickedItem.Tag);
		}

		private void OnMenuVisibleChanged(object sender, EventArgs e) {
			if (!menu.Visible) {
				menuOpen = false;
			}
		}

        //Brush brush_TabBackColor = SystemBrushes.GradientInactiveCaption;
        //Color color_TabBackColor = Color.FromArgb(232, 232, 232);
        SolidBrush brush_TabBackColor = new SolidBrush(Color.FromArgb(232, 232, 232));
        protected override void OnPaint(PaintEventArgs e)
        {
            SetDefaultSelected();
            Rectangle clientRectangle = base.ClientRectangle;
            clientRectangle.Width--;
            clientRectangle.Height--;
            DEF_START_POS_forOnPaint = DEF_START_POS;
            e.Graphics.DrawRectangle(SystemPens.ControlDark, clientRectangle);
            e.Graphics.FillRectangle(Brushes.White, clientRectangle);
            e.Graphics.FillRectangle(brush_TabBackColor, new Rectangle(clientRectangle.X, clientRectangle.Y, clientRectangle.Width, DEF_BUTTON_HEIGHT));
            //int radius = 5;
            //e.Graphics.DrawRoundRectangle(SystemPens.ControlDark, clientRectangle, radius);
            //e.Graphics.FillRoundRectangle(Brushes.White, clientRectangle, radius);
            //e.Graphics.FillRoundRectangle(brush_TabBackColor,new Rectangle(clientRectangle.X, clientRectangle.Y, clientRectangle.Width, DEF_BUTTON_HEIGHT),radius);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 0; i < Items.Count; i++)
            {
                BrowserTabStripItem fATabStripItem = Items[i];
                if (fATabStripItem.Visible || base.DesignMode)
                {
                    OnCalcTabPage(e.Graphics, fATabStripItem);
                    fATabStripItem.IsDrawn = false;
                    OnDrawTabButton(e.Graphics, fATabStripItem);
                }
            }
            if (selectedItem != null)
            {
                OnDrawTabButton(e.Graphics, selectedItem);
            }
            if (Items.DrawnCount == 0 || Items.VisibleCount == 0)
            {
                //e.Graphics.DrawLine(SystemPens.ControlDark, new Point(0, 28), new Point(base.ClientRectangle.Width, 28));
                e.Graphics.DrawLine(Pens.Red, new Point(0, DEF_BUTTON_HEIGHT), new Point(base.ClientRectangle.Width, DEF_BUTTON_HEIGHT));
            }
            else if (SelectedItem != null && SelectedItem.IsDrawn)
            {
                //var lineColorPen = SystemPens.ControlDark;
                var lineColorPen = new Pen( color_TablButton_inActiveBG);

                //int num = (int)(SelectedItem.StripRect.Height / 4f);
                Point point = new Point((int)SelectedItem.StripRect.Left - TabRadius, DEF_BUTTON_HEIGHT);
                e.Graphics.DrawLine(lineColorPen, new Point(0, DEF_BUTTON_HEIGHT), point);
                point.X += (int)SelectedItem.StripRect.Width
                    //+ num * 2;
                    + TabRadius+2;
                e.Graphics.DrawLine(lineColorPen, point, new Point(base.ClientRectangle.Width, DEF_BUTTON_HEIGHT));
            }
            if (SelectedItem != null && SelectedItem.CanClose)
            {
                closeButton.IsVisible = true;
                closeButton.CalcBounds(selectedItem);
                closeButton.Draw(e.Graphics);
            }
            else
            {
                closeButton.IsVisible = false;
            }
        }

        /// <summary>
        /// ready for future use, (_ [] X) , if we put Tabs and Close/Minimize buttons on TitleBar 
        /// </summary>
        int atRight_ReservedWidth = 250; 
        private void OnCalcTabPage(Graphics g, BrowserTabStripItem currentItem) {
			//_ = Font;
			int calcWidth = 0;
			if (currentItem.Title == "+") {
				calcWidth = AddButtonWidth;
			}
			else {
				calcWidth = (base.Width -atRight_ReservedWidth - (AddButtonWidth + 20)) / (items.Count - 1);
				if (calcWidth > MaxTabSize) {
					calcWidth = MaxTabSize;
				}
			}
			RectangleF rectangleF2 = (currentItem.StripRect = new RectangleF(DEF_START_POS_forOnPaint, 3f, calcWidth, DEF_BUTTON_HEIGHT));
			DEF_START_POS_forOnPaint += calcWidth;
		}

		private SizeF MeasureTabWidth(Graphics g, BrowserTabStripItem currentItem, Font currentFont) {
			SizeF result = g.MeasureString(currentItem.Title, currentFont, new SizeF(200f, DEF_BUTTON_HEIGHT), sf);
			result.Width += 25f;
			return result;
		}

        bool styleNotPill = true;
        int TabRadius = 8;
		//Color color_TablButton_ActiveBG = Color.White;
		Color color_TablButton_ActiveBG = Color.FromArgb(247, 247, 247);
		//Color color_TablButton_inActiveBG = SystemColors.GradientInactiveCaption;
		//Color color_TablButton_inActiveBG = Color.FromArgb(222, 225, 231);
		Color color_TablButton_inActiveBG = Color.FromArgb(232, 232, 232);
        /// <summary>
        /// ********  Draws The Tab Header. aka button
        /// </summary>
        /// <param name="g"></param>
        /// <param name="currentItem"></param>
        private void OnDrawTabButton(Graphics g, BrowserTabStripItem currentItem)
        {
            Items.IndexOf(currentItem);
            Font font = Font;

            bool isActiveTab = currentItem == SelectedItem;
            bool is_atRightof_ActiveTab = Items.IndexOf(currentItem) == Items.IndexOf(selectedItem)+1; 

            RectangleF stripRect = currentItem.StripRect;
            var sr = stripRect;
            SolidBrush brush = new SolidBrush((currentItem == SelectedItem) ? color_TablButton_ActiveBG : color_TablButton_inActiveBG);
            Pen pen = new Pen((currentItem == SelectedItem) ? color_TablButton_ActiveBG : color_TablButton_inActiveBG);
            var tabDrawnRect = new RectangleF(sr.Left, 1, sr.Width - 2, sr.Height - 2);
			//tabDrawnRect.Height += 2; // hides bottom Line of Rect.
			tabDrawnRect.Y += 8;  
            tabDrawnRect.Height -= 8;
			if(styleNotPill)
                tabDrawnRect.Height += 2;

            ////--Draw Rectange Tabs
            //g.FillRectangle(brush, tabDrawnRect);
            //g.DrawRectangle(SystemPens.ControlDark, tabDrawnRect);
           
			////--Draw Pill Style Tabs
            //g.FillRoundRectangle(brush, tabDrawnRect,TabRadius);
            //g.DrawRoundRectangle(SystemPens.ControlDark, tabDrawnRect, TabRadius);

            ////--Draw Rounded Chrome Tabs
            var tabpathNew =
				isActiveTab ?
				tabDrawnRect.CreateTabPath_Roundtop_RoundBottomOut(TabRadius) :
				tabDrawnRect.CreateTabPath_roundAll(TabRadius);

            g.FillPath(brush, tabpathNew);
            //g.DrawPath(SystemPens.ControlDark, tabpathNew);
			//--white color requires more work...
            g.DrawPath(pen , tabpathNew);
			//--draw ,tab seperator line:   | TAB1 | TAB2
			if (!isActiveTab && !is_atRightof_ActiveTab) 
			{
                int margin = 14;
                sr.Y += 2; //move rect down
                g.DrawLine(SystemPens.ControlDark, sr.X, sr.Y + margin, sr.X, sr.Y + sr.Height - margin);
            }
			
			//g.DrawPath(SystemPens.ControlDark, graphicsPath);
			if (currentItem == SelectedItem)
            {
                //g.DrawLine(new Pen(brush), sr_left + 19f, sr_height + 2f, sr_left + sr_width - 1f, sr_height + 2f);
            }
           
			var ForeColorSel = ForeColor;
			//if (Debugger.IsAttached)
			//    ForeColorSel = Color.DarkCyan;

			//margin for Tab Text, Vertically Center
			tabDrawnRect.X += 10;
			tabDrawnRect.Width -= 10;
            //sf.LineAlignment = StringAlignment.Center; 
            if (currentItem == SelectedItem)
            {
                tabDrawnRect.Width -= 25;// dont overflow ellipsis "..." under the activeTab.CloseButton.
                //g.DrawRectangle(Pens.Cyan, tabDrawnRect);
                g.DrawString(currentItem.Title, font, new SolidBrush(ForeColorSel), tabDrawnRect, sf);
            }
            else
            {
                g.DrawString(currentItem.Title, font, new SolidBrush(ForeColorSel), tabDrawnRect, sf);
            }
            currentItem.IsDrawn = true;
        }
      

		//int TabButton_Height = 10;
		int TabButton_Height2 = 30;
		private void UpdateLayout() {
            //sf.Trimming = StringTrimming.Character;
            sf.Trimming = StringTrimming.EllipsisCharacter;
			sf.FormatFlags = StringFormatFlags.NoWrap;
			//sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft; //this line causes multiline.//is this arabic??
            sf.LineAlignment = StringAlignment.Center;

            stripButtonRect = new Rectangle(0, 0, base.ClientSize.Width - 40 - 2, TabButton_Height);
			base.DockPadding.Top = DEF_BUTTON_HEIGHT+1;
			base.DockPadding.Bottom = 1;
			base.DockPadding.Right = 1;
			base.DockPadding.Left = 1;
		}

		private void OnCollectionChanged(object sender, CollectionChangeEventArgs e) {
			BrowserTabStripItem fATabStripItem = (BrowserTabStripItem)e.Element;
			if (e.Action == CollectionChangeAction.Add) {
				Controls.Add(fATabStripItem);
				OnTabStripItemChanged(new TabStripItemChangedEventArgs(fATabStripItem, BrowserTabStripItemChangeTypes.Added));
			}
			else if (e.Action == CollectionChangeAction.Remove) {
				Controls.Remove(fATabStripItem);
				OnTabStripItemChanged(new TabStripItemChangedEventArgs(fATabStripItem, BrowserTabStripItemChangeTypes.Removed));
			}
			else {
				OnTabStripItemChanged(new TabStripItemChangedEventArgs(fATabStripItem, BrowserTabStripItemChangeTypes.Changed));
			}
			UpdateLayout();
			Invalidate();
		}

        public bool ShouldSerializeFont() => !Font?.Equals(defaultFont) ?? false;
        public bool ShouldSerializeSelectedItem() => true;
        public bool ShouldSerializeItems() => items.Count > 0;
        public new void ResetFont() => Font = defaultFont;
        public void BeginInit() => isIniting = true;
        public void EndInit() => isIniting = false;

        protected override void Dispose(bool disposing) {
			if (disposing) {
				items.CollectionChanged -= OnCollectionChanged;
				menu.ItemClicked -= OnMenuItemClicked;
				menu.VisibleChanged -= OnMenuVisibleChanged;
				foreach (BrowserTabStripItem item in items) {
					if (item != null && !item.IsDisposed) {
						item.Dispose();
					}
				}
				if (menu != null && !menu.IsDisposed) {
					menu.Dispose();
				}
				if (sf != null) {
					sf.Dispose();
				}
			}
			base.Dispose(disposing);
		}
	}
}