﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlLibrary.Helper;
using RenrenCoreWrapper.CacheService;
using RenrenCoreWrapper.Facades;
using RenrenCoreWrapper.Framework.CacheService;
using RenrenCoreWrapper.Helper;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace ControlLibrary
{
    public class ClipImageBrush : Control
    {
        private ImageBrush clipImageBrushCache = null;
        private Grid gridRoot = null;
        private ICacheSerivce<BitmapImage> clipImageBrushCacheSerivce = null;
        private ICacheChip<BitmapImage> chip = null;
        private string hashKey = string.Empty;

        private Storyboard sbVisible = null;
        private Storyboard sbNotVisible = null;

        private Uri baseUri = new Uri("ms-appx://");

        /// <summary>
        /// 重试次数
        /// </summary>
        private volatile int RetryCount = 0;

        /// <summary>
        /// 是否是直接走外网
        /// </summary>
        private bool IsOnlineImage = false;

        //图片打开成功的代理
        public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);
        /// <summary>
        /// 图片打开成功的事件
        /// </summary>
        public event RoutedEventHandler ImageOpened;

        //图片打开失败的代理
        public delegate void ExceptionRoutedEventHandler(object sender, ExceptionRoutedEventArgs e);
        /// <summary>
        /// 图片打开失败的事件
        /// </summary>
        public event ExceptionRoutedEventHandler ImageFailed;

        //图片下载的实时代理
        public delegate void ImageDownLoadProgressHandler(int downloadValue);
        /// <summary>
        /// 图片下载的实时事件
        /// </summary>
        public event ImageDownLoadProgressHandler ImageDownLoadProgress;

        //动画完成的实时代理
        public delegate void AnimationCompleteHandler();
        /// <summary>
        /// 动画完成的实时事件
        /// </summary>
        public event AnimationCompleteHandler AnimationComplete;


        private bool isImageComplete = false;
        /// <summary>
        /// 判断图片是否已经完成
        /// </summary>
        public bool IsImageComplete
        {
            get
            {
                return isImageComplete;
            }
            set
            {
                isImageComplete = value;
            }
        }

        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(string), typeof(ClipImageBrush), new PropertyMetadata(null, new PropertyChangedCallback(onSourcePropertyChanged)));

        private static void onSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null && !string.IsNullOrEmpty(ClipImageBrush.Source))
            {
                ClipImageBrush.ChangeSourceAndCacheType();
            }
            //如果赋值空或者Null，是希望它清空图片，为了虚拟化
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null && string.IsNullOrEmpty(ClipImageBrush.Source))
            {
                ClipImageBrush.AnimationBeginStop();
                ClipImageBrush.gridRoot.Opacity = 1.0;
                ClipImageBrush.clipImageBrushCache.ImageSource = null;
                ClipImageBrush.Width = 0;
                ClipImageBrush.Height = 0;
            }
        }

        public bool IsAnimation
        {
            get { return (bool)GetValue(IsAnimationProperty); }
            set { SetValue(IsAnimationProperty, value); }
        }

        public static readonly DependencyProperty IsAnimationProperty = DependencyProperty.Register("IsAnimation", typeof(bool), typeof(ClipImageBrush), new PropertyMetadata(false, new PropertyChangedCallback(onIsAnimationPropertyChanged)));

        private static void onIsAnimationPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null && !string.IsNullOrEmpty(ClipImageBrush.Source))
            {
                ClipImageBrush.ChangeSourceAndCacheType();
            }
        }

        public TimeSpan AnimationTime
        {
            get { return (TimeSpan)GetValue(AnimationTimeProperty); }
            set { SetValue(AnimationTimeProperty, value); }
        }

        public static readonly DependencyProperty AnimationTimeProperty = DependencyProperty.Register("AnimationTime", typeof(TimeSpan), typeof(ClipImageBrush), new PropertyMetadata(TimeSpan.FromSeconds(2), new PropertyChangedCallback(onAnimationTimePropertyChanged)));

        private static void onAnimationTimePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null && !string.IsNullOrEmpty(ClipImageBrush.Source))
            {
                ClipImageBrush.ChangeSourceAndCacheType();
            }
        }

        public AnimationType AnimationType
        {
            get { return (AnimationType)GetValue(AnimationTypeProperty); }
            set { SetValue(AnimationTypeProperty, value); }
        }

        public static readonly DependencyProperty AnimationTypeProperty = DependencyProperty.Register("AnimationType", typeof(AnimationType), typeof(ClipImageBrush), new PropertyMetadata(AnimationType.AanimationFadeOut, new PropertyChangedCallback(OnAnimationTypePropertyChanged)));

        private static void OnAnimationTypePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null && !string.IsNullOrEmpty(ClipImageBrush.Source))
            {
                ClipImageBrush.ChangeSourceAndCacheType();
            }
        }

        public CacheImageDateType CacheType
        {
            get { return (CacheImageDateType)GetValue(CacheTypeProperty); }
            set { SetValue(CacheTypeProperty, value); }
        }

        public static readonly DependencyProperty CacheTypeProperty = DependencyProperty.Register("CacheType", typeof(CacheImageDateType), typeof(ClipImageBrush), new PropertyMetadata(CacheImageDateType.CacheHead, new PropertyChangedCallback(onCacheTypePropertyChanged)));

        private static void onCacheTypePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null && !string.IsNullOrEmpty(ClipImageBrush.Source))
            {
                ClipImageBrush.ChangeSourceAndCacheType();
            }
        }

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(ClipImageBrush), new PropertyMetadata(Stretch.UniformToFill, new PropertyChangedCallback(onStretchPropertyChanged)));

        private static void onStretchPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null)
            {
                ClipImageBrush.clipImageBrushCache.Stretch = ClipImageBrush.Stretch;
            }
        }

        public AlignmentX AlignmentX
        {
            get { return (AlignmentX)GetValue(AlignmentXProperty); }
            set { SetValue(AlignmentXProperty, value); }
        }

        public static readonly DependencyProperty AlignmentXProperty = DependencyProperty.Register("AlignmentX", typeof(AlignmentX), typeof(ClipImageBrush), new PropertyMetadata(AlignmentX.Center, new PropertyChangedCallback(onAlignmentXPropertyChanged)));

        private static void onAlignmentXPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null)
            {

            }
        }

        public AlignmentY AlignmentY
        {
            get { return (AlignmentY)GetValue(AlignmentYProperty); }
            set { SetValue(AlignmentYProperty, value); }
        }

        public static readonly DependencyProperty AlignmentYProperty = DependencyProperty.Register("AlignmentY", typeof(AlignmentY), typeof(ClipImageBrush), new PropertyMetadata(AlignmentY.Center, new PropertyChangedCallback(onAlignmentYPropertyChanged)));

        private static void onAlignmentYPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null)
            {

            }
        }

        public bool IsCacheImage
        {
            get { return (bool)GetValue(IsCacheImageProperty); }
            set { SetValue(IsCacheImageProperty, value); }
        }

        public static readonly DependencyProperty IsCacheImageProperty = DependencyProperty.Register("IsCacheImage", typeof(bool), typeof(ClipImageBrush), new PropertyMetadata(true, new PropertyChangedCallback(onIsCacheImagePropertyChanged)));

        private static void onIsCacheImagePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ClipImageBrush = sender as ClipImageBrush;
            if (ClipImageBrush != null && ClipImageBrush.clipImageBrushCache != null && !string.IsNullOrEmpty(ClipImageBrush.Source))
            {
                ClipImageBrush.ChangeSourceAndCacheType();
            }
        }

        /// <summary>
        /// 是否开启图片只有下完，才显示新图，还是下载的时候，图片是空置状态 （false为下完才显示新图，true为空置状态，默认为false）
        /// </summary>
        public bool IsDisplayOldImage
        {
            get { return (bool)GetValue(IsDisplayOldImageProperty); }
            set { SetValue(IsDisplayOldImageProperty, value); }
        }

        public static readonly DependencyProperty IsDisplayOldImageProperty = DependencyProperty.Register("IsDisplayOldImage", typeof(bool), typeof(ClipImageBrush), new PropertyMetadata(false, new PropertyChangedCallback(onIsDisplayOldImagePropertyChanged)));

        private static void onIsDisplayOldImagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var clipImageBrush = d as ClipImageBrush;
            if (clipImageBrush != null && clipImageBrush.clipImageBrushCache != null && !string.IsNullOrEmpty(clipImageBrush.Source))
            {
                clipImageBrush.ChangeSourceAndCacheType();
            }
        }

        private void ChangeSourceAndCacheType()
        {
            if (this.IsAnimation)
            {
                if (this.AnimationType == ControlLibrary.AnimationType.AanimationFadeOut)
                {
                    this.gridRoot.Opacity = 0;
                }
                else
                {
                    this.planeProjection.RotationX = 90;
                }
                CreateAnimationBegin();
            }
            else
            {
                this.gridRoot.Opacity = 1.0;
                this.planeProjection.RotationX = 0;
            }

            BitmapImage bi = new BitmapImage();
            if (this.IsDisplayOldImage)
            {
                this.clipImageBrushCache.ImageSource = bi;
            }
            var reg = @"http(s)?://([\w-]+\.)+[\w-]+(/[\w-./?%&=]*)?";
            Regex regex = new Regex(reg, RegexOptions.IgnoreCase);
            if (regex.IsMatch(this.Source))
            {
                if (this.IsCacheImage)
                {
                    var af = new RenrenCodeFacader().GetServiceAbstractFactry();
                    var cacheFactory = af.CreateCacheServiceFactry();
                    //var imageCache = cacheFactory.CreateImageCacheByServiceType(ServiceType.AlbumListServiceType);
                    /*var imageCache = cacheFactory.CreateImageCacheByServiceType(this.CacheType);*/
                    var imageCache = cacheFactory.CreateImageCacheByServiceType(CacheImageDateManage.CacheTypeToString(this.CacheType));

                    hashKey = ApiHelper.ComputeMD5(this.Source);
                    //DateTime expTime = CacheImageDateManage.CacheTypeToDate((ServiceType)parameter);
                    //DateTime expTime = CacheImageDateManage.CacheTypeToDate(parameter.ToString());
                    clipImageBrushCacheSerivce = (imageCache as ICacheSerivce<BitmapImage>);

                    IProgress<int> progress = new Progress<int>((p) =>
                    {
                        if (this.ImageDownLoadProgress != null)
                        {
                            this.ImageDownLoadProgress(p);
                        }
                    });
                    chip = clipImageBrushCacheSerivce.CreateCacheChip(hashKey, DateTime.Now, progress);
                    this.IsImageComplete = clipImageBrushCacheSerivce.IsValid(chip);
                    bi = clipImageBrushCacheSerivce.Pick(chip).Result;
                    IsOnlineImage = false;
                }
                else
                {
                    bi.UriSource = new Uri(this.Source, UriKind.RelativeOrAbsolute);
                    //bi.DownloadProgress += (ss, ee) =>
                    //{
                    //    if (this.ImageDownLoadProgress != null)
                    //    {
                    //        this.ImageDownLoadProgress(ee.Progress);
                    //    }
                    //};
                    bi.DownloadProgress -= bi_DownloadProgress;
                    bi.DownloadProgress += bi_DownloadProgress;
                    IsOnlineImage = true;
                }
            }
            else
            {
                bi.UriSource = new Uri(this.BaseUri, this.Source);
                //if (this.ImageDownLoadProgress != null)
                //{
                //    this.ImageDownLoadProgress(100);
                //}
                bi.DownloadProgress -= bi_DownloadProgress;
                bi.DownloadProgress += bi_DownloadProgress;
            }
            if (!this.IsDisplayOldImage)
            {
                this.clipImageBrushCache.ImageSource = bi;
            }
        }

        private void bi_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            if (this.ImageDownLoadProgress != null)
            {
                this.ImageDownLoadProgress(e.Progress);
            }
        }

        /// <summary>
        /// 占停下载
        /// </summary>
        public void Pause()
        {
            chip.Pause();
        }

        /// <summary>
        /// 继续下载
        /// </summary>
        public void Resume()
        {
            chip.Resume();
        }

        /// <summary>
        /// 取消下载
        /// </summary>
        public void Cancel()
        {
            chip.Cancel();
        }

        public ClipImageBrush()
        {
            this.DefaultStyleKey = typeof(ClipImageBrush);
            sbVisible = new Storyboard();
            sbNotVisible = new Storyboard();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            clipImageBrushCache = this.GetTemplateChild("imageBrush") as ImageBrush;
            gridRoot = this.GetTemplateChild("gridRoot") as Grid;
            if (clipImageBrushCache != null && gridRoot != null)
            {
                this.clipImageBrushCache.Stretch = this.Stretch;
                clipImageBrushCache.ImageOpened -= clipImageBrushCache_ImageOpened;
                clipImageBrushCache.ImageOpened += clipImageBrushCache_ImageOpened;
                clipImageBrushCache.ImageFailed -= clipImageBrushCache_ImageFailed;
                clipImageBrushCache.ImageFailed += clipImageBrushCache_ImageFailed;
                this.AddProjection(gridRoot);
                if (!string.IsNullOrEmpty(this.Source))
                {
                    this.ChangeSourceAndCacheType();
                }

                if (!this.IsCacheImage)
                {
                    IsOnlineImage = true;
                }
            }
        }

        private PlaneProjection planeProjection = null;
        private void AddProjection(UIElement control)
        {
            if (control.Projection == null)
            {
                planeProjection = new PlaneProjection();
                planeProjection.RotationX = 0;
                planeProjection.CenterOfRotationX = planeProjection.CenterOfRotationY = 0.5;
                control.Projection = planeProjection;
            }
        }

        public Storyboard GetStart()
        {
            return sbVisible;
        }

        //简写动画方法(淡入) 与 （翻转）
        protected virtual void CreateAnimationBegin()
        {
            //sbVisible = new Storyboard();
            sbVisible.Stop();
            sbVisible.Children.Clear();
            sbVisible.Completed -= sbVisible_Completed;
            sbVisible.Completed += sbVisible_Completed;
            if (this.AnimationType == ControlLibrary.AnimationType.AanimationFadeOut)
            {
                DoubleAnimationUsingKeyFrames keyFramesOpacity = new DoubleAnimationUsingKeyFrames();
                Storyboard.SetTarget(keyFramesOpacity, gridRoot);
                Storyboard.SetTargetProperty(keyFramesOpacity, "(UIElement.Opacity)");
                KeyTime ktOpacity1 = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0));
                keyFramesOpacity.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = ktOpacity1, Value = 0 });
                KeyTime ktOpacity2 = KeyTime.FromTimeSpan(this.AnimationTime);
                keyFramesOpacity.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = ktOpacity2, Value = 1 });
                sbVisible.Children.Add(keyFramesOpacity);
            }
            else
            {
                DoubleAnimationUsingKeyFrames keyFramesRotationX = new DoubleAnimationUsingKeyFrames();
                Storyboard.SetTarget(keyFramesRotationX, gridRoot);
                Storyboard.SetTargetProperty(keyFramesRotationX, "(UIElement.Projection).(PlaneProjection.RotationX)");
                KeyTime ktRotationX1 = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0));
                keyFramesRotationX.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = ktRotationX1, Value = 90 });
                KeyTime ktRotationX2 = KeyTime.FromTimeSpan(this.AnimationTime);
                keyFramesRotationX.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = ktRotationX2,
                    Value = 360,
                    //EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseOut, Power = 4 }
                    EasingFunction = new CircleEase() { EasingMode = EasingMode.EaseOut }
                });
                sbVisible.Children.Add(keyFramesRotationX);
            }
        }

        private void sbVisible_Completed(object sender, object e)
        {
            if (this.AnimationComplete != null)
            {
                this.AnimationComplete();
            }
        }

        private void AnimationBeginStart()
        {
            if (sbVisible != null)
            {
                sbVisible.Begin();
            }
        }

        private void AnimationBeginStop()
        {
            if (sbVisible != null)
            {
                sbVisible.Stop();
            }
        }

        public Storyboard GetEnd()
        {
            return sbNotVisible;
        }

        //简写动画方法(浅出)
        protected virtual void CreateAnimationEnd()
        {
            //sbNotVisible = new Storyboard();
            sbNotVisible.Stop();
            sbNotVisible.Children.Clear();
            if (this.AnimationType == ControlLibrary.AnimationType.AanimationFadeOut)
            {
                DoubleAnimationUsingKeyFrames keyFramesOpacity = new DoubleAnimationUsingKeyFrames();
                Storyboard.SetTarget(keyFramesOpacity, gridRoot);
                Storyboard.SetTargetProperty(keyFramesOpacity, "(UIElement.Opacity)");
                KeyTime ktOpacity1 = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0));
                keyFramesOpacity.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = ktOpacity1, Value = 1 });
                KeyTime ktOpacity2 = KeyTime.FromTimeSpan(this.AnimationTime);
                keyFramesOpacity.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = ktOpacity2, Value = 0 });
                sbNotVisible.Children.Add(keyFramesOpacity);
            }
            else
            {

            }
        }

        private void AnimationEndStart()
        {
            if (sbNotVisible != null)
            {
                sbNotVisible.Begin();
            }
        }

        private void AnimationEndStop()
        {
            if (sbNotVisible != null)
            {
                sbNotVisible.Stop();
            }
        }

        async void clipImageBrushCache_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (this.IsCacheImage)
            {
                RetryCount++;
                if (RetryCount < 4)
                {
                    var image = sender as ImageBrush;
                    var imageUri = this.Source;
                    await image.Dispatcher.RunIdleAsync((p) =>
                    {
                        var unused = Task.Factory.StartNew(async () =>
                        {
                            ICacheChip<BitmapImage> chip = clipImageBrushCacheSerivce.CreateCacheChip(hashKey, null);
                            (chip as ImageCacheChip).AddImageUpdate(image);
                            await clipImageBrushCacheSerivce.Add(chip, imageUri);
                        });
                    });
                }

                if (RetryCount == 4)
                {
                    IsOnlineImage = true;
                    BitmapImage bi = new BitmapImage(new Uri(this.Source, UriKind.RelativeOrAbsolute));
                    bi.DownloadProgress += (ss, ee) =>
                    {
                        if (this.ImageDownLoadProgress != null)
                        {
                            this.ImageDownLoadProgress(ee.Progress);
                        }
                    };
                    bi.ImageOpened += (s1, e1) =>
                    {
                        clipImageBrushCache.ImageSource = bi;
                    };
                }

                if (RetryCount > 4)
                {
                    if (this.ImageFailed != null)
                    {
                        this.ImageFailed(sender, e);
                    }
                    return;
                }
            }
            else
            {
                if (this.ImageFailed != null)
                {
                    this.ImageFailed(sender, e);
                }
            }
        }

        void clipImageBrushCache_ImageOpened(object sender, RoutedEventArgs e)
        {
            //if (this.ImageDownLoadProgress != null)
            //{
            //    this.ImageDownLoadProgress(100);
            //}

            this.RetryCount = 0;
            if (this.IsAnimation)
            {
                AnimationBeginStop();
                AnimationBeginStart();
            }
            else
            {
                AnimationBeginStop();
                gridRoot.Opacity = 1.0;
            }

            if (this.ImageOpened != null)
            {
                //this.ImageOpened(this, e);
                this.ImageOpened(sender, e);
            }
        }

        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// 保存图片到系统的文件夹
        /// </summary>
        /// <returns>保存成功返回true,失败返回false</returns>
        public async Task<bool> SaveImage(string fileName = "")
        {
            if (!IsOnlineImage)
            {
                if (clipImageBrushCacheSerivce != null && chip != null)
                {
                    try
                    {
                        StorageFolder storageFolder = KnownFolders.PicturesLibrary;
                        StorageFile storageFile = clipImageBrushCacheSerivce.PickFile(chip).Result as StorageFile;
                        if (string.IsNullOrEmpty(fileName))
                        {
                            await storageFile.CopyAsync(storageFolder, storageFile.Name + ".jpg", NameCollisionOption.GenerateUniqueName);
                        }
                        else
                        {
                            await storageFile.CopyAsync(storageFolder, fileName + ".jpg", NameCollisionOption.GenerateUniqueName);
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    StorageFolder storageFolder = KnownFolders.PicturesLibrary;
                    StorageFile storageFile = null;
                    var rass = RandomAccessStreamReference.CreateFromUri(new Uri(this.Source, UriKind.RelativeOrAbsolute));
                    IRandomAccessStreamWithContentType streamRandom = await rass.OpenReadAsync();
                    using (Stream tempStream = streamRandom.GetInputStreamAt(0).AsStreamForRead())
                    {
                        MemoryStream ms = new MemoryStream();
                        await tempStream.CopyToAsync(ms);
                        byte[] bytes = ms.ToArray();

                        storageFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(ApiHelper.ComputeMD5(this.Source), CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteBytesAsync(storageFile, bytes);
                    }

                    if (string.IsNullOrEmpty(fileName))
                    {
                        await storageFile.CopyAsync(storageFolder, storageFile.Name + ".jpg", NameCollisionOption.GenerateUniqueName);
                    }
                    else
                    {
                        await storageFile.CopyAsync(storageFolder, fileName + ".jpg", NameCollisionOption.GenerateUniqueName);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 保存图片到任意的文件夹
        /// </summary>
        /// <returns>保存成功返回true,失败返回false</returns>
        public async Task<bool> SaveFileImage()
        {
            if (!IsOnlineImage)
            {
                if (clipImageBrushCacheSerivce != null && chip != null)
                {
                    try
                    {
                        StorageFile storageFile = clipImageBrushCacheSerivce.PickFile(chip).Result as StorageFile;
                        FileSavePicker savePicker = new FileSavePicker();
                        savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                        savePicker.FileTypeChoices.Add("图片类型", new List<string>() { ".jpg" });
                        savePicker.SuggestedFileName = storageFile.Name;
                        StorageFile file = await savePicker.PickSaveFileAsync();
                        if (file != null)
                        {
                            CachedFileManager.DeferUpdates(file);
                            await storageFile.CopyAndReplaceAsync(file);//, file.Name, NameCollisionOption.GenerateUniqueName);
                            FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                            if (status == FileUpdateStatus.Complete)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    StorageFile storageFile = null;
                    var rass = RandomAccessStreamReference.CreateFromUri(new Uri(this.Source, UriKind.RelativeOrAbsolute));
                    IRandomAccessStreamWithContentType streamRandom = await rass.OpenReadAsync();
                    using (Stream tempStream = streamRandom.GetInputStreamAt(0).AsStreamForRead())
                    {
                        MemoryStream ms = new MemoryStream();
                        await tempStream.CopyToAsync(ms);
                        byte[] bytes = ms.ToArray();

                        storageFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(ApiHelper.ComputeMD5(this.Source), CreationCollisionOption.ReplaceExisting);
                        await FileIO.WriteBytesAsync(storageFile, bytes);
                    }

                    FileSavePicker savePicker = new FileSavePicker();
                    savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                    savePicker.FileTypeChoices.Add("图片类型", new List<string>() { ".jpg" });

                    savePicker.SuggestedFileName = storageFile.Name;
                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        CachedFileManager.DeferUpdates(file);
                        await storageFile.CopyAndReplaceAsync(file);//, file.Name, NameCollisionOption.GenerateUniqueName);
                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                        if (status == FileUpdateStatus.Complete)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        private async void Gif(IRandomAccessStream fileStream)
        {     
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
            BitmapFrame frame = await decoder.GetFrameAsync(0);
            var frameProperties = await frame.BitmapProperties.GetPropertiesAsync(new List<string>());

            var imageDescriptionProperties = await(frameProperties["/imgdesc"].Value as BitmapPropertiesView).GetPropertiesAsync(new List<string>() { "/Top", "/Left", "/Width", "/Height" });
            int top = Int32.Parse(imageDescriptionProperties["/Top"].Value.ToString());
            int left = Int32.Parse(imageDescriptionProperties["/Left"].Value.ToString());
            int width = Int32.Parse(imageDescriptionProperties["/Width"].Value.ToString());
            int height = Int32.Parse(imageDescriptionProperties["/Height"].Value.ToString());

            var gifControlExtensionProperties = await(frameProperties["/grctlext"].Value as BitmapPropertiesView).GetPropertiesAsync(new List<string>() { "/Delay", "/UserInputFlag" });
            TimeSpan delay = TimeSpan.FromSeconds(Double.Parse(gifControlExtensionProperties["/Delay"].Value.ToString()) / 100); // delay is in 1/100s of a second
            bool userInputFlag = bool.Parse(gifControlExtensionProperties["/UserInputFlag"].Value.ToString());
        }
    }
}
