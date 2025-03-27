using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using LibAPNG;
using YooAsset;

public class APNGPlayer : MonoBehaviour
{
    /// <summary>
    /// ͼƬ��Դ
    /// </summary>
    public enum ImageSource
    {
        FromStreamingAssets,
        FromFile,
        FromHttp,
        FromYooAssets,
    }

    /// <summary>
    /// Textureʹ��ģʽ�����Ż����
    /// ���ű�ʾÿ֡��������һ��Texture�ϻ���
    /// ���ű�ʾ��Ϊÿ֡������������һ��Texture
    /// </summary>
    public enum TextureMode
    {
        SingleTexture,
        MultiTexture,
    }

    /// <summary>
    /// ����״̬
    /// </summary>
    public enum LoadState
    {
        UNLOADED,//δ����
        LOADING,//������
        PROCESSING,//������
        READY,//׼�����
        ERROR,//����
    }

    /// <summary>
    /// ����״̬
    /// </summary>
    public enum PlayState
    {
        STOPED,//ֹͣ
        PLAYING,//����
        PAUSED,//��ͣ
    }

    public class APNGFrame
    {
        //��ǰ֡����
        public int index;
        public Frame frame;
        //��ǰ֡ͼ������
        public Color32[] pixels;
        //��ǰ֡����ʱ��
        public float duration;
        //ָ����һ֡����֮ǰ�Ի������Ĳ���
        public DisposeOps disposeOp;
        //ָ�����Ƶ�ǰ֮֡ǰ�Ի������Ĳ���
        public BlendOps blendOp;
        //��ǰ֡���ؿ�
        public uint width;
        //��ǰ֡���ظ�
        public uint height;
        //��ǰ֡x��������ƫ��
        public uint xOffset;
        //��ǰ֡y��������ƫ��
        public uint yOffset;
        //��ǰ֡Texture
        public Texture2D texture;

        public APNGFrame Clone()
        {
            var result = new APNGFrame();
            result.index = this.index;
            result.frame = this.frame;
            result.pixels = this.pixels;
            result.duration = this.duration;
            result.disposeOp = this.disposeOp;
            result.blendOp = this.blendOp;
            result.width = this.width;
            result.height = this.height;
            result.xOffset = this.xOffset;
            result.yOffset = this.yOffset;
            result.texture = this.texture;
            return result;
        }
    }

    class ImagePixels
    {
        private uint mWidth;
        public uint width
        {
            get => mWidth;
        }
        private uint mHeight;
        public uint height
        {
            get => mHeight;
        }
        private Color32[] mPixels;
        public Color32[] pixels
        {
            get => mPixels;
        }
        public ImagePixels(uint width, uint height)
        {
            mWidth = width;
            mHeight = height;
            Clear();
        }

        /// <summary>
        /// ����������ص�
        /// </summary>
        public void Clear()
        {
            mPixels = new Color32[mWidth * mHeight];
        }

        /// <summary>
        /// ���һ����������
        /// </summary>
        /// <param name="x">ˮƽ����ʼ���أ�������˳��</param>
        /// <param name="y">��ֱ����ʼ���أ����µ���˳��</param>
        /// <param name="width">���ؿ�</param>
        /// <param name="height">���ظ�</param>
        public void ClearRect(uint x, uint y, uint width, uint height)
        {
            //var startIndex = y * mWidth + x;
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    var index = (y + j) * mWidth + x + i;
                    var color = mPixels[index];
                    color.r = 0;
                    color.g = 0;
                    color.b = 0;
                    color.a = 0;
                }
            }
        }

        /// <summary>
        /// �������ص�����
        /// </summary>
        /// <param name="pixels">���ص�����</param>
        /// <param name="x">ˮƽ����ʼ���أ�������˳��</param>
        /// <param name="y">��ֱ����ʼ���أ����µ���˳��</param>
        /// <param name="width">���ؿ�</param>
        /// <param name="height">���ظ�</param>
        public void SetPixels(Color32[] pixels, uint x, uint y, uint width, uint height)
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    var index = (y + j) * mWidth + x + i;
                    mPixels[index] = pixels[j * width + i];
                }
            }
        }

        /// <summary>
        /// �������ص�����
        /// </summary>
        /// <param name="pixels"></param>
        public void SetPixels(Color32[] pixels)
        {
            SetPixels(pixels, 0, 0, mWidth, mHeight);
        }

        /// <summary>
        /// ��ȡ���ص�����
        /// </summary>
        /// <param name="x">ˮƽ����ʼ���أ�������˳��</param>
        /// <param name="y">��ֱ����ʼ���أ����ϵ���˳��</param>
        /// <param name="width">���ؿ�</param>
        /// <param name="height">���ظ�</param>
        /// <returns>���ص�����</returns>
        public Color32[] GetPixels(uint x, uint y, uint width, uint height)
        {
            var result = new Color32[width * height];
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    var index = (y + j) * mWidth + x + i;
                    result[j * width + i] = mPixels[index];
                }
            }
            return result;
        }

        /// <summary>
        /// ��ȡ���ص�����
        /// </summary>
        /// <returns>���ص�����</returns>
        public Color32[] GetPixels()
        {
            return GetPixels(0, 0, mWidth, mHeight);
        }
    }

    [Tooltip("YooAssetPackage名")]
    public string YooAssetPackageName;

    [Tooltip("APNGͼƬ����·��")]
    public string imagePath;
    [Tooltip("APNGͼƬ��Դ")]
    public ImageSource imageSource;
    [Tooltip("Textureʹ��ģʽ")]
    public TextureMode textureMode = TextureMode.MultiTexture;
    [Tooltip("ָ��APNGͼ������Ҫ��ֵ��Material")]
    public List<Material> materials = new List<Material>();
    [Tooltip("ָ��APNGͼ������Ҫ��ֵ��RawImage")]
    public List<RawImage> rawImages = new List<RawImage>();
    [Tooltip("�Ƿ���ű�����ִ��")]
    public bool runOnStart = true;
    [Tooltip("�Ƿ��Զ����ţ�Ϊtrue�������ɺ�������ʼ���ţ�Ϊfalse�����ֶ�����Play()�ſ�ʼ����")]
    public bool autoPlay = true;
    [Tooltip("�����ٶȱ���")]
    [Min(0.1f)]
    public float playSpeed = 1.0f;
    [Tooltip("����ѭ�����Ŵ�����0��ʾ������")]
    [Min(0)]
    public int maxLoopCount = 0;

    private LoadState mLoadState = LoadState.UNLOADED;
    public bool isUnloaded
    {
        get { return mLoadState == LoadState.UNLOADED; }
    }
    public bool isLoading
    {
        get { return mLoadState == LoadState.LOADING; }
    }
    public bool isProcessing
    {
        get { return mLoadState == LoadState.PROCESSING; }
    }
    public bool isReady
    {
        get { return mLoadState == LoadState.READY; }
    }
    public bool isError
    {
        get { return mLoadState == LoadState.ERROR; }
    }

    private PlayState mPlayState = PlayState.STOPED;
    public bool isStoped
    {
        get { return mPlayState == PlayState.STOPED; }
    }
    public bool isPlaying
    {
        get { return mPlayState == PlayState.PLAYING; }
    }
    public bool isPaused
    {
        get { return mPlayState == PlayState.PAUSED; }
    }

    private APNG mApng;
    private List<APNGFrame> mFrames = new List<APNGFrame>();
    private uint mWidth;
    public uint imageWidth
    {
        get { return mWidth; }
    }
    private uint mHeight;
    public uint imageHeight
    {
        get { return mHeight; }
    }
    private APNGFrame mPrevFrame;
    private Texture2D mTexture;
    [Obsolete]
    public Texture2D texture
    {
        get { return mTexture; }
    }
    public Texture2D currentTexture
    {
        get { return mTexture; }
    }
    private ImagePixels mImagePixels;
    private float mLastTime = 0.0f;
    private int mCurrentFrameIndex = -1;
    public int currentFrameIndex
    {
        get { return mCurrentFrameIndex; }
    }
    public int framesNumber
    {
        get { return mFrames.Count; }
    }
    private int mLoopCount = 0;

    public delegate void OnReady(APNGPlayer player);

    public delegate void OnError(APNGPlayer player, string error);

    public delegate void OnChanged(APNGPlayer player, int frameIndex, APNGFrame frame);

    public event OnReady onReady;
    public event OnError onError;
    public event OnChanged onChanged;

    // Start is called before the first frame update
    void Start()
    {
        if (runOnStart)
        {
            Run();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isReady && isPlaying)
        {
            checkNextFrame();
        }
    }

    /// <summary>
    /// ��ʼ����
    /// </summary>
    public void Run()
    {
        if (imagePath == null)
        {
            Debug.LogWarning("The url is null, can't call Run() method.");
            return;
        }
        if (isUnloaded || isError)
            StartCoroutine(load());
        else
            Debug.LogWarning("This player load state is " + mLoadState + ", can't call Run() method.");
    }

    //����ͼƬ������
    private IEnumerator load()
    {
        mLoadState = LoadState.LOADING;

        if (imageSource == ImageSource.FromYooAssets)
        {
            if (YooAssetPackageName != null)
            {
                string error = "YooAssetPackageName = null!";
                mLoadState = LoadState.ERROR;
                Debug.LogError(error);
                onError?.Invoke(this, error);
            }
            else
            {
                mLoadState = LoadState.PROCESSING;

                ResourcePackage package = YooAssets.TryGetPackage(YooAssetPackageName);
                AssetHandle handle = package.LoadAssetAsync<Texture2D>(imagePath);
                yield return handle;
                if (handle.Status == EOperationStatus.Succeed)
                {
                    Texture2D texture = handle.AssetObject as Texture2D;
                    if (texture != null)
                    {
                        // 获取原始纹理数据（Raw Data）
                        byte[] rawData = texture.GetRawTextureData();

                        Debug.Log($"Raw Data Length: {rawData.Length}");

                        yield return loadAPNG(rawData);
                    }
                    else
                    {
                        string error = $"YooAssets LoadAssetAsync texture failed! {YooAssetPackageName}, {imagePath}";
                        mLoadState = LoadState.ERROR;
                        Debug.LogError(error);
                        onError?.Invoke(this, error);
                    }
                }
                else
                {
                    string error = $"YooAssets LoadAssetAsync failed! {YooAssetPackageName}, {imagePath}";
                    mLoadState = LoadState.ERROR;
                    Debug.LogError(error);
                    onError?.Invoke(this, error);
                }
            }

        }
        else
        {
            Uri uri;
            if (imageSource == ImageSource.FromStreamingAssets)
                uri = new Uri(Path.Combine(Application.streamingAssetsPath, imagePath));
            else
                uri = new Uri(imagePath);
            using (UnityWebRequest www = UnityWebRequest.Get(uri))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    string error = "Get " + imagePath + " error: " + www.error;
                    Debug.LogError(error);
                    mLoadState = LoadState.ERROR;
                    onError?.Invoke(this, error);
                    yield break;
                }

                //��ʼ���ݴ���
                mLoadState = LoadState.PROCESSING;

                yield return loadAPNG(www.downloadHandler.data);
            }
        }


    }

    private IEnumerator loadAPNG(byte[] bytes)
    {
        try
        {
            //����APNGͼƬ����
            mApng = new APNG(bytes);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            mLoadState = LoadState.ERROR;
            onError?.Invoke(this, e.Message);
            yield break;
        }
        yield return null;

        //��ȡͼƬ����
        mWidth = (uint)mApng.IHDRChunk.Width;
        mHeight = (uint)mApng.IHDRChunk.Height;
        //����Texture
        mTexture = new Texture2D(mApng.IHDRChunk.Width, mApng.IHDRChunk.Height);
        //����ImagePixels
        mImagePixels = new ImagePixels(mWidth, mHeight);
        yield return null;

        mFrames.Clear();
        int count = 0;
        for (int i = 0; i < mApng.Frames.Length; i++)
        {
            var frame = mApng.Frames[i];

            //���ɵ�ǰ֡Texture
            var data = frame.GetStream().ToArray();
            var texture = new Texture2D(1, 1);
            texture.LoadImage(data);

            var apngFrame = new APNGFrame();
            apngFrame.index = i;
            apngFrame.frame = frame;
            //��ȡ��ǰ֡Texture��������
            apngFrame.pixels = texture.GetPixels32();
            //���㵱ǰ֡����ʱ��
            apngFrame.duration = (float)frame.fcTLChunk.DelayNum / (float)frame.fcTLChunk.DelayDen;
            apngFrame.disposeOp = frame.fcTLChunk.DisposeOp;
            apngFrame.blendOp = frame.fcTLChunk.BlendOp;
            apngFrame.width = frame.fcTLChunk.Width;
            apngFrame.height = frame.fcTLChunk.Height;
            apngFrame.xOffset = frame.fcTLChunk.XOffset;
            //����yOffset����Texture���õ��Ǵ��µ���˳������������Ҫ��תyOffset
            apngFrame.yOffset = mHeight - (frame.fcTLChunk.YOffset + apngFrame.height);

            mFrames.Add(apngFrame);

            Destroy(texture);

            count++;
            //ÿ����10֡��ִ��һ��yield return�Ա����߳�����
            if (count % 10 == 0)
                yield return null;
        }
        //yield return null;

        mLoadState = LoadState.READY;
        Debug.Log("This player is ready.");
        onReady?.Invoke(this);

        //autoPlayΪtrue��ֱ�ӿ�ʼ����
        if (autoPlay)
        {
            Play();
        }
        //Ϊfalse�����õ�ǰ����Ϊ��һ֡
        else
        {
            setCurrentFrameImpl(0);
        }
    }

    /// <summary>
    /// ����Ѽ��ص�����
    /// </summary>
    public void Clear()
    {
        if (!isReady)
        {
            Debug.LogWarning("This player is not ready, can't call Clear() method.");
            return;
        }
        mPlayState = PlayState.STOPED;
        mLoadState = LoadState.UNLOADED;
        mCurrentFrameIndex = -1;
        mFrames.Clear();
        mApng = null;
        if (mTexture != null)
        {
            Destroy(mTexture);
            mTexture = null;
        }
    }

    /// <summary>
    /// ��ʼ����
    /// </summary>
    public void Play()
    {
        if (!isReady)
        {
            Debug.LogWarning("This player is not ready, can't call Play() method.");
            return;
        }
        //if (materials.Count == 0 && rawImages.Count == 0)
        //{
        //    Debug.LogWarning("The materials and rawImages count is 0, can't call Play() method.");
        //    return;
        //}
        if (isPlaying)
            return;
        if (mCurrentFrameIndex == -1)
            setCurrentFrameImpl(0);
        mLastTime = Time.time;
        mPlayState = PlayState.PLAYING;
    }

    /// <summary>
    /// ֹͣ����
    /// </summary>
    public void Stop()
    {
        if (!isReady)
        {
            Debug.LogWarning("This player is not ready, can't call Stop() method.");
            return;
        }
        if (isStoped)
            return;
        mPlayState = PlayState.STOPED;
        //�ָ�����һ֡
        setCurrentFrameImpl(0);
        //���ö���ѭ������
        mLoopCount = 0;
    }

    /// <summary>
    /// ��ͣ����
    /// </summary>
    public void Pause()
    {
        if (!isReady)
        {
            Debug.LogWarning("This player is not ready, can't call Pause() method.");
            return;
        }
        if (isStoped || isPaused)
            return;
        mPlayState = PlayState.PAUSED;
    }

    /// <summary>
    /// ���¿�ʼ����
    /// </summary>
    public void Restart()
    {
        Stop();
        Start();
    }

    //public void SetCurrentFrame(int index)
    //{
    //    if (!isReady)
    //    {
    //        Debug.LogWarning("This player is not ready, can't call SetCurrentFrame() method.");
    //        return;
    //    }
    //    if (material == null && rawImage == null)
    //    {
    //        Debug.LogWarning("The material and rawImage is null, can't call SetCurrentFrame() method.");
    //        return;
    //    }
    //    if (index < 0 || index >= this.framesNumber)
    //    {
    //        Debug.LogWarning("SetCurrentFrame error, index " + index + " is out of bounds [" + 0 + ", " + this.framesNumber + ")");
    //        return;
    //    }
    //    setCurrentFrameImpl(index);
    //}

    //���õ�ǰ֡
    private void setCurrentFrameImpl(int index)
    {
        if (mCurrentFrameIndex == index)
            return;
        mCurrentFrameIndex = index;
        var frame = mFrames[index];
        if (textureMode == TextureMode.SingleTexture || (textureMode == TextureMode.MultiTexture && frame.texture == null))
        {
            //��һ֡
            if (index == 0)
            {
                //���Ƶ�һ֡ǰ�����������������
                mImagePixels.Clear();
                //�ÿ���һ֡
                mPrevFrame = null;
            }
            //������һ֡
            if (mPrevFrame != null)
            {
                switch (mPrevFrame.disposeOp)
                {
                    case DisposeOps.APNGDisposeOpNone://����������ֱ�ӻ���
                        break;
                    case DisposeOps.APNGDisposeOpBackground://�����һ֡����
                        mImagePixels.ClearRect(mPrevFrame.xOffset, mPrevFrame.yOffset, mPrevFrame.width, mPrevFrame.height);
                        break;
                    case DisposeOps.APNGDisposeOpPrevious://�ָ�Ϊ��һ֡����ǰ������
                        mImagePixels.SetPixels(mPrevFrame.pixels, mPrevFrame.xOffset, mPrevFrame.yOffset, mPrevFrame.width, mPrevFrame.height);
                        break;
                }
            }
            mPrevFrame = frame.Clone();
            //�洢��ǰ�Ļ������ݣ�������һ֡����ǰ�ָ�������
            if (mPrevFrame.disposeOp == DisposeOps.APNGDisposeOpPrevious)
                mPrevFrame.pixels = mImagePixels.GetPixels(mPrevFrame.xOffset, mPrevFrame.yOffset, mPrevFrame.width, mPrevFrame.height);
            //��յ�ǰ֡���������
            if (mPrevFrame.blendOp == BlendOps.APNGBlendOpSource)
                mImagePixels.ClearRect(mPrevFrame.xOffset, mPrevFrame.yOffset, mPrevFrame.width, mPrevFrame.height);
            //���Ƶ�ǰ֡
            mImagePixels.SetPixels(frame.pixels, mPrevFrame.xOffset, mPrevFrame.yOffset, mPrevFrame.width, mPrevFrame.height);
            if (textureMode == TextureMode.SingleTexture)
            {
                if (frame.texture == null)
                    frame.texture = mTexture;
            }
            else if (textureMode == TextureMode.MultiTexture)
            {
                if (frame.texture == null)
                    frame.texture = new Texture2D(mApng.IHDRChunk.Width, mApng.IHDRChunk.Height);
                mTexture = frame.texture;
            }
            //�����ƺõ��������ø�Texture
            mTexture.SetPixels32(mImagePixels.pixels);
            mTexture.Apply();
        }
        else
        {
            mTexture = frame.texture;
        }
        //ΪMaterial��RawImage��ֵ
        foreach (var material in materials)
        {
            material.mainTexture = mTexture;
        }
        foreach (var rawImage in rawImages)
        {
            rawImage.texture = mTexture;
        }

        onChanged?.Invoke(this, index, frame);
    }

    //��ȡ��һ֡����
    private int getNextFrameIndex()
    {
        var index = mCurrentFrameIndex;
        index++;
        //���������һ֡
        if (index >= framesNumber)
        {
            index = 0;
            mLoopCount++;
        }
        return index;
    }

    //����Ƿ���ת��һ֡
    private void checkNextFrame()
    {
        //���������ѭ����������ѭ������������������ֹͣ����
        if (maxLoopCount > 0 && mLoopCount >= maxLoopCount)
        {
            Stop();
            return;
        }
        var nowTime = Time.time;
        if (nowTime - mLastTime >= mFrames[mCurrentFrameIndex].duration / playSpeed)
        {
            setCurrentFrameImpl(getNextFrameIndex());
            mLastTime = nowTime;
        }
    }
}
