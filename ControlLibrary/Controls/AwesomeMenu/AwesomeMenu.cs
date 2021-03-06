﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace ControlLibrary
{
    public class AwesomeMenu : Grid
    {
        #region Private Fileds
        private List<AwesomMenuItem> MenuItems;
        private int _flag = 0;
        protected AwesomeMenuType Type
        {
            get { return _type; }
            set
            {
                _type = value;
            }
        }
        private Point _startPoint;
        private AwesomeMenuType _type = AwesomeMenuType.AwesomeMenuTypeDefault;
        private Point START_POINT = new Point(100, 130);
        private const double NEAR_RADIUS = 130d;
        private const double END_RADIUS = 140d;
        private const double FAR_RADIUS = 160d;
        private const double BETWEEN_RADIUS = 50d;
        private TimeSpan TIMEOFFSET = TimeSpan.FromSeconds(0.05);
        private DispatcherTimer _timer;
        private AwesomMenuItem _addButton;
        private string _addUri;
        private string _addUriHighlited;
        private bool _tapToDismissItem = false;
        private bool _isExpanding;
        private AwesomeMenuRadianType awesomeMenuRadianType = AwesomeMenuRadianType.AwesomeMenuRadian90;
        private double menuItemSpacing = 0.0;

        /// <summary>
        /// Indicator whether dismiss the menuitem when tapped
        /// </summary>
        private bool TapToDissmissItem
        {
            get { return _tapToDismissItem; }
            set { _tapToDismissItem = value; }
        }
        #endregion

        #region public Attribute
        /// <summary>
        /// 控制面板展开与关闭
        /// </summary>
        public bool IsExpanding
        {
            get { return _isExpanding; }
            set
            {
                _isExpanding = value;
                SetExpanding(_isExpanding);
            }
        }

        /// <summary>
        /// 设置按钮以多少度展现
        /// </summary>
        public AwesomeMenuRadianType AwesomeMenuRadianType
        {
            get
            {
                return awesomeMenuRadianType;
            }
            set
            {
                awesomeMenuRadianType = value;
                ConversionMenuItemSpacing();
                SetType(Type);
            }
        }

        /// <summary>
        /// 每个Item的间距
        /// </summary>
        public double MenuItemSpacing
        {
            get
            {
                return menuItemSpacing;
            }
            set
            {
                menuItemSpacing = value;
                if (this.AwesomeMenuRadianType == AwesomeMenuRadianType.AwesomeMenuRadianNone)
                {
                    ConversionMenuItemSpacing();
                    SetType(Type);
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetType(AwesomeMenuType menuType = AwesomeMenuType.AwesomeMenuTypeDefault)
        {
            Type = menuType;
            int dx = 1;
            int dy = 1;
            bool isTwoDirections = true;
            if (MenuItems != null)
            {
                switch (Type)
                {
                    case AwesomeMenuType.AwesomeMenuTypeUpAndRight:
                        dx = 1;
                        dy = -1;
                        break;
                    case AwesomeMenuType.AwesomeMenuTypeUpAndLeft:
                        dx = -1;
                        dy = -1;
                        break;
                    case AwesomeMenuType.AwesomeMenuTypeDownAndRight:
                        dy = 1;
                        dx = 1;
                        break;
                    case AwesomeMenuType.AwesomeMenuTypeDownAndLeft:
                        dy = 1;
                        dx = -1;
                        break;
                    case AwesomeMenuType.AwesomeMenuTypeUp:
                        isTwoDirections = false;
                        dx = 0;
                        dy = -1;
                        break;
                    case AwesomeMenuType.AwesomeMenuTypeDown:
                        isTwoDirections = false;
                        dx = 0;
                        dy = 1;
                        break;
                    case AwesomeMenuType.AwesomeMenuTypeLeft:
                        isTwoDirections = false;
                        dx = -1;
                        dy = 0;
                        break;
                    case AwesomeMenuType.AwesomeMenuTypeRight:
                        isTwoDirections = false; dx = 1; dy = 0;
                        break;
                    default:
                        break;
                }

                int nCount = MenuItems.Count;
                for (int i = 0; i < nCount; i++)
                {
                    var item = MenuItems[i];
                    item.StratPoint = _startPoint;
                    if (isTwoDirections)
                    {
                        item.EndPoint = CalulateDynamaticPoint(i, END_RADIUS, nCount, dx, dy);
                        item.NearPoint = CalulateDynamaticPoint(i, NEAR_RADIUS, nCount, dx, dy);
                        item.FarPoint = CalulateDynamaticPoint(i, FAR_RADIUS, nCount, dx, dy);
                    }
                    else
                    {
                        var j = i + 1;
                        item.EndPoint = new Point(_startPoint.X + dx * j * BETWEEN_RADIUS, _startPoint.Y + dy * j * BETWEEN_RADIUS);
                        item.NearPoint = new Point(_startPoint.X + dx * j * (BETWEEN_RADIUS - 15), _startPoint.Y + dy * j * (BETWEEN_RADIUS - 15));
                        item.FarPoint = new Point(_startPoint.X + dx * j * (BETWEEN_RADIUS + 20), _startPoint.Y + dy * j * (BETWEEN_RADIUS + 20));
                    }
                    item.ItemTransfrom.TranslateX = item.StratPoint.X;
                    item.ItemTransfrom.TranslateY = item.StratPoint.Y;
                }

            }
        }

        public void SetStartPoint(Point pt)
        {
            this.Children.Clear();
            InitAddButton();
            _startPoint = START_POINT = pt;
            _addButton.ItemTransfrom.TranslateX = _startPoint.X;
            _addButton.ItemTransfrom.TranslateY = _startPoint.Y;
            //_addButton.ItemTransfrom.CenterX = _addButton.ItemTransfrom.CenterY = 0.5;
            InitMenuItem();
            SetType(Type);
        }
        #endregion

        #region Actions
        public Action<AwesomeMenu, int> ActionDisMiss;
        public Action<AwesomMenuItem> ActionItemOpen;
        public Action<AwesomMenuItem> ActionItemClose;
        #endregion

        #region Structure
        public AwesomeMenu(Rect rect, List<AwesomMenuItem> menuItems, string addUri, string addUriHigtlighted, AwesomeMenuType menuType = AwesomeMenuType.AwesomeMenuTypeDefault)
        {
            this.Width = rect.Width;
            this.Height = rect.Height;
            this.MenuItems = menuItems;
            this._addUri = addUri;
            this._addUriHighlited = addUriHigtlighted;
            this.Background = new SolidColorBrush(Colors.Transparent);
            this.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            this.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
            this.Tapped -= AwesomeMenu_Tapped;
            this.Tapped += AwesomeMenu_Tapped;
            InitAddButton();
            InitMenuItem();
            //原来没有叫设置按钮角度属性
            //SetType(menuType);
            //现在有了更改一下代码
            Type = menuType;
            this.AwesomeMenuRadianType = AwesomeMenuRadianType.AwesomeMenuRadian90;
        }
        #endregion

        #region Private methods
        private void InitMenuItem()
        {
            if (MenuItems != null || MenuItems.Count > 0)
            {
                int nCount = MenuItems.Count;
                for (int i = 0; i < nCount; i++)
                {
                    var item = MenuItems[i];
                    item.StratPoint = START_POINT;
                    item.EndPoint = new Point(0, 0);//CalulateInitPoint(i, END_RADIUS, nCount);
                    item.NearPoint = new Point(0, 0);//CalulateInitPoint(i, NEAR_RADIUS, nCount);
                    item.FarPoint = new Point(0, 0);//CalulateInitPoint(i, FAR_RADIUS, nCount);
                    item.Tag = i;
                    item.ClickMenuItem -= Item_ClickMenuItem;
                    item.ClickMenuItem += Item_ClickMenuItem;
                    //item.ItemTransfrom.CenterX = START_POINT.X;
                    //item.ItemTransfrom.CenterY = START_POINT.Y;

                    this.Children.Add(item);

                }
            }
        }

        private void InitAddButton()
        {
            _addButton = new AwesomMenuItem(_addUri, _addUriHighlited);
            _addButton.ItemTransfrom.TranslateX = _startPoint.X;
            _addButton.ItemTransfrom.TranslateY = _startPoint.Y;
            _addButton.Tag = 999;
            Canvas.SetZIndex(_addButton, 10);
            _addButton.ClickMenuItem -= Item_ClickMenuItem;
            _addButton.ClickMenuItem += Item_ClickMenuItem;
            this.Children.Add(_addButton);
        }

        private void Expand()
        {
            if (_flag >= MenuItems.Count)
            {
                if (_timer.IsEnabled)
                {
                    _timer.Stop();
                    _timer = null;
                    return;
                }
            }

            int tag = _flag;
            if (tag >= MenuItems.Count || tag < 0)
                return;
            var item = MenuItems[tag];
            Duration duration = TimeSpan.FromSeconds(0.5);

            var sb = new Storyboard();
            sb.Duration = duration;

            var da = new DoubleAnimationUsingKeyFrames();
            da.Duration = duration;

            var keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.ItemTransfrom.Rotation;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.0);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = 90;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.15);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = 180;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.3);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = 0;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.5);
            da.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "Rotation");
            sb.Children.Add(da);

            da = new DoubleAnimationUsingKeyFrames();
            da.Duration = duration;

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.StratPoint.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.FarPoint.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.15);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.NearPoint.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.3);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.EndPoint.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.5);
            da.KeyFrames.Add(keyframe);


            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "TranslateX");
            sb.Children.Add(da);

            da = new DoubleAnimationUsingKeyFrames();
            da.Duration = duration;

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.StratPoint.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.FarPoint.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.15);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.NearPoint.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.3);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.EndPoint.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.5);
            da.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "TranslateY");
            sb.Children.Add(da);

            sb.Begin();
            sb.Completed += (o, a) =>
            {
                //System.Diagnostics.Debug.WriteLine(item.EndPoint.X)
                item.ItemTransfrom.TranslateX = item.EndPoint.X;
                item.ItemTransfrom.TranslateY = item.EndPoint.Y;
                item.ItemTransfrom.Rotation = 0;
                sb.Stop();
                sb.Children.Clear();
                da = null;
                sb = null;
                if (ActionItemOpen != null)
                    ActionItemOpen(item);
            };
            _flag++;
        }

        private void Close()
        {
            if (_flag < 0)
            {
                _timer.Stop();
                _timer = null;
                return;
            }

            int tag = _flag;
            AwesomMenuItem item = null;
            //if (tag >= MenuItems.Count)
            //{
            //    item = _addButton;
            //}
            //else
            //item = MenuItems[tag];

            if (tag - 1 < 0)
            {
                return;
            }
            item = MenuItems[tag - 1];

            Duration duration = TimeSpan.FromSeconds(0.5);

            var sb = new Storyboard();
            sb.Duration = duration;

            var da = new DoubleAnimationUsingKeyFrames();
            da.Duration = duration;

            var keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = 0;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.0);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = 180;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.25);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = 0;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.5);
            da.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "Rotation");
            sb.Children.Add(da);

            da = new DoubleAnimationUsingKeyFrames();
            da.Duration = duration;

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.EndPoint.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.FarPoint.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.25);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.StratPoint.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.5);
            da.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "TranslateX");
            sb.Children.Add(da);

            da = new DoubleAnimationUsingKeyFrames();
            da.Duration = duration;

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.EndPoint.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.FarPoint.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.25);
            da.KeyFrames.Add(keyframe);

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = item.StratPoint.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.5);
            da.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "TranslateY");
            sb.Children.Add(da);

            sb.Begin();
            sb.Completed += (o, a) =>
            {
                item.ItemTransfrom.TranslateX = item.StratPoint.X;
                item.ItemTransfrom.TranslateY = item.StratPoint.Y;
                item.ItemTransfrom.Rotation = 0;
                sb.Stop();
                sb.Children.Clear();
                da = null;
                sb = null;
                if (ActionItemClose != null)
                    ActionItemClose(item);
            };
            _flag--;
        }

        private void SetExpanding(bool isExpanding)
        {
            _isExpanding = isExpanding;

            //rotate add button //弧度
            double angle = this.IsExpanding ? -Math.PI / 4 : 0.0d;
            //角度
            angle = angle * 180;
            var duration = TimeSpan.FromSeconds(0.5);
            var sb = new Storyboard();
            sb.Duration = duration;
            _addButton.ItemTransfrom.CenterX = _addButton.ItemTransfrom.CenterY = 0.5;
            var da = GetDoubleAnimation(duration, _addButton.ItemTransfrom.Rotation, angle);
            Storyboard.SetTarget(da, _addButton.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "Rotation");
            sb.Children.Add(da);
            sb.Begin();
            sb.Completed += (o, a) =>
            {
                _addButton.ItemTransfrom.Rotation = angle;
                sb.Stop();
                sb.Children.Clear();
                da = null;
                sb = null;
            };

            //Init and Start Timer
            if (_timer == null)
            {
                _flag = this.IsExpanding ? 0 : MenuItems.Count;
                _timer = new DispatcherTimer();
                _timer.Interval = TIMEOFFSET;
                _timer.Tick += (o, a) =>
                {
                    if (IsExpanding)
                    {
                        Expand();
                    }
                    else
                    {
                        Close();
                    }
                };
                _timer.Start();
            }
        }

        /// <summary>
        /// 计算角度
        /// </summary>
        private void ConversionMenuItemSpacing()
        {
            switch (this.AwesomeMenuRadianType)
            {
                case AwesomeMenuRadianType.AwesomeMenuRadian90:
                    {
                        this.menuItemSpacing = (90 / (MenuItems.Count - 1));
                    }
                    break;
                case AwesomeMenuRadianType.AwesomeMenuRadian180:
                    {
                        this.menuItemSpacing = (180 / (MenuItems.Count - 1));
                    }
                    break;
                case AwesomeMenuRadianType.AwesomeMenuRadian360:
                    {
                        this.menuItemSpacing = (360 / (MenuItems.Count - 1));
                    }
                    break;
                default:
                    if (this.MenuItemSpacing == 0.0 || double.IsNaN(this.MenuItemSpacing))
                    {
                        this.menuItemSpacing = (90 / (MenuItems.Count - 1));
                    }
                    break;
            }
        }
        #endregion

        #region Event Handler
        // Unexpanded the menu while expanded
        void AwesomeMenu_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // Confilct with the menuitem T_T
            //if (_isExpanding)
            //{
            //    _isExpanding = false;
            //}
            if (this.IsExpanding)
            {
                this.IsExpanding = false;
            }
        }

        private void Item_ClickMenuItem(AwesomMenuItem item)
        {
            if (item.Tag.Equals(_addButton.Tag))
            {
                this.IsExpanding = !this.IsExpanding;
                return;
            }

            if (!TapToDissmissItem)
            {
                //blowup current button
                Point pt = new Point(item.ItemTransfrom.TranslateX, item.ItemTransfrom.TranslateY);
                var blowUpStory = GetBlowUpAnimation(item, pt);
                blowUpStory.Begin();
                blowUpStory.Completed += (o, a) =>
                {
                    item.ItemTransfrom.TranslateX = _startPoint.X;
                    item.ItemTransfrom.TranslateY = _startPoint.Y;
                    item.ItemTransfrom.ScaleX = 1;
                    item.ItemTransfrom.ScaleY = 1;
                    item.Opacity = 1;
                    blowUpStory.Stop();
                    blowUpStory.Children.Clear();
                    blowUpStory = null;
                };

                foreach (var otherItem in MenuItems)
                {
                    if (item != otherItem)
                    {
                        Point p = new Point(otherItem.ItemTransfrom.TranslateX, otherItem.ItemTransfrom.TranslateY);
                        var shrinkStory = GetShrinkAnimation(otherItem, p);
                        shrinkStory.Begin();
                        shrinkStory.Completed += (o, a) =>
                        {
                            otherItem.ItemTransfrom.TranslateX = _startPoint.X;
                            otherItem.ItemTransfrom.TranslateY = _startPoint.Y;
                            otherItem.ItemTransfrom.ScaleX = 1;
                            otherItem.ItemTransfrom.ScaleY = 1;
                            otherItem.Opacity = 1;
                            shrinkStory.Stop();
                            shrinkStory.Children.Clear();
                            shrinkStory = null;
                        };
                    }
                }
                _isExpanding = false;

                //收起做的动画
                //弧度
                double angle = this.IsExpanding ? -Math.PI / 4 : 0;
                //角度
                angle = angle * 180;
                Duration duration = TimeSpan.FromSeconds(0.5);
                _addButton.RenderTransformOrigin = new Point(0.5, 0.5);
                var da = GetDoubleAnimation(duration, _addButton.ItemTransfrom.Rotation, angle);
                var sb = new Storyboard();
                //sb.Duration = duration;
                Storyboard.SetTarget(da, _addButton.ItemTransfrom);
                Storyboard.SetTargetProperty(da, "Rotation");
                sb.Children.Add(da);
                sb.Begin();
                sb.Completed += (o, a) =>
                {
                    sb.Stop();
                    _addButton.ItemTransfrom.Rotation = angle;
                    sb.Children.Clear();
                    sb = null;
                };
            }


            if (ActionDisMiss != null)
                ActionDisMiss(this, Convert.ToInt32(item.Tag));

        }
        #endregion

        #region Helper Methods
        Point CalulateInitPoint(int index, double radius, int nCount)
        {
            return new Point(
                        _startPoint.X + radius * Math.Sin(index * Math.PI / 2 / (nCount - 1)),
                        _startPoint.Y - radius * Math.Cos(index * Math.PI / 2 / (nCount - 1))
                        );
        }

        Point CalulateDynamaticPoint(int index, double radius, int nCount, int dx, int dy)
        {
            //return new Point(
            //            _startPoint.X + dx * radius * Math.Sin(index * Math.PI / 2 / (nCount - 1)),
            //            _startPoint.Y + dy * radius * Math.Cos(index * Math.PI / 2 / (nCount - 1))
            //            );
            return new Point(
                        _startPoint.X + dx * radius * Math.Sin(index * Math.PI * MenuItemSpacing / 180),
                        _startPoint.Y + dy * radius * Math.Cos(index * Math.PI * MenuItemSpacing / 180)
                        );
        }

        Storyboard GetBlowUpAnimation(AwesomMenuItem item, Point pt)
        {
            Duration duration = TimeSpan.FromSeconds(0.3);
            var sb = new Storyboard();
            sb.Duration = duration;

            var keyFrams = new DoubleAnimationUsingKeyFrames();
            keyFrams.Duration = duration;

            var keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = pt.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.3);
            keyFrams.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(keyFrams, item.ItemTransfrom);
            Storyboard.SetTargetProperty(keyFrams, "TranslateX");
            sb.Children.Add(keyFrams);

            keyFrams = new DoubleAnimationUsingKeyFrames();
            keyFrams.Duration = duration;

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = pt.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.3);
            keyFrams.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(keyFrams, item.ItemTransfrom);
            Storyboard.SetTargetProperty(keyFrams, "TranslateY");
            sb.Children.Add(keyFrams);


            var da = GetDoubleAnimation(duration, 3, 1);
            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "ScaleX");
            sb.Children.Add(da);

            da = GetDoubleAnimation(duration, 3, 1);
            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "ScaleY");
            sb.Children.Add(da);

            da = GetDoubleAnimation(duration, 1, 0);
            Storyboard.SetTarget(da, item);
            Storyboard.SetTargetProperty(da, "(UIElement.Opacity)");
            sb.Children.Add(da);

            return sb;
        }

        Storyboard GetShrinkAnimation(AwesomMenuItem item, Point pt)
        {
            Duration duration = TimeSpan.FromSeconds(0.3);
            var sb = new Storyboard();
            sb.Duration = duration;

            var keyFrams = new DoubleAnimationUsingKeyFrames();
            keyFrams.Duration = duration;

            var keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = pt.X;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.3);
            keyFrams.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(keyFrams, item.ItemTransfrom);
            Storyboard.SetTargetProperty(keyFrams, "TranslateX");
            sb.Children.Add(keyFrams);

            keyFrams = new DoubleAnimationUsingKeyFrames();
            keyFrams.Duration = duration;

            keyframe = new EasingDoubleKeyFrame();
            keyframe.Value = pt.Y;
            keyframe.KeyTime = TimeSpan.FromSeconds(0.3);
            keyFrams.KeyFrames.Add(keyframe);

            Storyboard.SetTarget(keyFrams, item.ItemTransfrom);
            Storyboard.SetTargetProperty(keyFrams, "TranslateY");
            sb.Children.Add(keyFrams);


            var da = GetDoubleAnimation(duration, 0.01, 1);
            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "ScaleX");
            sb.Children.Add(da);

            da = GetDoubleAnimation(duration, 0.01, 1);
            Storyboard.SetTarget(da, item.ItemTransfrom);
            Storyboard.SetTargetProperty(da, "ScaleY");
            sb.Children.Add(da);

            da = GetDoubleAnimation(duration, 1, 0);
            Storyboard.SetTarget(da, item);
            Storyboard.SetTargetProperty(da, "(UIElement.Opacity)");
            sb.Children.Add(da);

            return sb;
        }

        DoubleAnimation GetDoubleAnimation(Duration duration, double from, double to)
        {
            var da = new DoubleAnimation();
            da.Duration = duration;
            da.From = from;
            da.To = to;
            da.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
            return da;
        }
        #endregion
    }
}
