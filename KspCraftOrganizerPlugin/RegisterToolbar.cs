using UnityEngine;
using ToolbarControl_NS;
using KSP_Log;

namespace KspCraftOrganizer
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        // Initialize and load all these values at startup (when getting to the MainMenu), this
        // saves both memory and cpu when in the mod 
        //
        internal static Log Log = null;
        internal static Texture2D settingsTextureBtn = null;
        internal static GUIStyle _toggleButtonStyleFalse;
        internal static GUIStyle _toggleButtonStyleTrue;
        internal static GUIStyle _warningLabelStyle;
        internal static GUIStyle settingsBtnStyle;
        static private GUISkin _skin = null;
        static private string skinName = "";
        public GUISkin skin { get { return _skin; } set { _skin = value; } }

        internal static GUIStyle fadeStyle;

        internal static GUIStyle _dropdownListStyle;
        internal static GUIStyle itemStyleNormal;
        internal static GUIStyle itemStyleHighlight;
        static private Texture2D hoverBackgroundTexture = KspNalCommon.UiUtils.createSingleColorTexture(new Color(200, 200, 200));

        internal static GUIStyle nameStyle;
        internal static GUIStyle descStyle;

        internal static GUIStyle tooltipBackgroundStyle;

        internal static GUIStyle tagsStyle;
        internal static GUIStyle craftNameStyle;
        internal static GUIStyle goodLabelStyle;
        internal static Color tagsColor;

        internal static Texture2D folderTextureBtn, folderUpTextureBtn;

        internal static GUIStyle buttonStyleYellow;
        internal static GUIStyle buttonStyleGreen;
        internal static GUIStyle buttonStyleOrange;

        void Awake()
        {
            if (Log == null)
#if DEBUG
                Log = new Log("CraftOrganizer", Log.LEVEL.INFO);
#else
                Log = new Log("CraftOrganizer", Log.LEVEL.ERROR);
#endif

        }

        void Start()
        {
            ToolbarControl.RegisterMod(KspCraftOrganizerMain.MODID, KspCraftOrganizerMain.MODNAME);
            ToolbarControl.RegisterMod(KspCraftOrganizerMain.MODTAGNAME, KspCraftOrganizerMain.MODTAGNAME);
        }

        bool Initted = false;
        void OnGUI()
        {
            if (!Initted)
            {
                UpdateStyles(GUI.skin, "Default");
                Initted = true;
            }
        }


        internal static void UpdateStyles(string style)
        {
            if (style == null)
                style = "";
            if (style == "KSP")
                UpdateStyles(HighLogic.Skin, style);
            else
                UpdateStyles(GUI.skin, style);
        }

        static Color HtmlToColor(string htmlValue)
        {
            Color newCol = Color.gray;

            if (ColorUtility.TryParseHtmlString(htmlValue, out newCol))
            {
                return newCol;
            }
            return newCol;
        }

        internal static void UpdateStyles(GUISkin newSkin, string newSkinName)
        {
            if (skinName != newSkinName)
            {
                skinName = newSkinName;

                _skin = newSkin;
                GUIStyle buttonStyle = _skin.button;

                settingsTextureBtn = KspNalCommon.UiUtils.loadIcon("settings.png");

                folderTextureBtn = KspNalCommon.UiUtils.loadIcon("folder.png");
                folderUpTextureBtn = KspNalCommon.UiUtils.loadIcon("folderUp.png");

                _toggleButtonStyleFalse = new GUIStyle(buttonStyle);
                _toggleButtonStyleFalse.hover = _toggleButtonStyleFalse.normal;
                _toggleButtonStyleFalse.active = _toggleButtonStyleFalse.normal;

                _toggleButtonStyleTrue = new GUIStyle(buttonStyle);
                _toggleButtonStyleTrue.normal = _toggleButtonStyleTrue.active;
                _toggleButtonStyleTrue.hover = _toggleButtonStyleTrue.active;

                _warningLabelStyle = new GUIStyle(_skin.label);
                //_warningLabelStyle.normal.textColor = new Color(1, 0.2f, 0.2f);
                //_warningLabelStyle.normal.textColor = new Color(0.6f, 0.38f, 0.3f);
                _warningLabelStyle.normal.textColor = new Color(0.99f, 0.57f, 0.6f, 1.0f);

                settingsBtnStyle = new GUIStyle(_skin.button);
                settingsBtnStyle.padding.left = 0;
                settingsBtnStyle.padding.right = 0;
                settingsBtnStyle.padding.top = 0;
                settingsBtnStyle.padding.bottom = 0;

                fadeStyle = new GUIStyle(_skin.box);

                Texture2D backgroundTexture = new Texture2D(1, 1);
                backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
                backgroundTexture.wrapMode = TextureWrapMode.Repeat;
                backgroundTexture.Apply();
                fadeStyle.normal.background = backgroundTexture;

                _dropdownListStyle = new GUIStyle(GUI.skin.button);
                GUIContent content = new GUIContent("X");
                float dropDownArrowSize = _dropdownListStyle.CalcSize(content).y;
                _dropdownListStyle.padding.right = (int)dropDownArrowSize;
                _dropdownListStyle.alignment = TextAnchor.MiddleLeft;

                itemStyleNormal = new GUIStyle();
                itemStyleHighlight = new GUIStyle();

                itemStyleHighlight.hover.textColor = Color.black;
                itemStyleHighlight.normal.background = hoverBackgroundTexture;

                itemStyleHighlight.hover.background = hoverBackgroundTexture;
                itemStyleHighlight.onHover.background = hoverBackgroundTexture;
                itemStyleHighlight.hover.textColor = Color.black;
                itemStyleHighlight.onHover.textColor = Color.black;

                itemStyleHighlight.padding = new RectOffset(4, 4, 4, 4);


                itemStyleNormal.hover.background = hoverBackgroundTexture;
                itemStyleNormal.onHover.background = hoverBackgroundTexture;
                itemStyleNormal.hover.textColor = Color.black;
                itemStyleNormal.onHover.textColor = Color.black;

                itemStyleNormal.padding = new RectOffset(4, 4, 4, 4);

                nameStyle = new GUIStyle(_skin.label);
                descStyle = new GUIStyle(_skin.label);

                tooltipBackgroundStyle = new GUIStyle(_skin.box);
                Texture2D backgroundTexture2 = new Texture2D(1, 1);
                backgroundTexture2.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
                backgroundTexture2.wrapMode = TextureWrapMode.Repeat;
                backgroundTexture2.Apply();
                tooltipBackgroundStyle.normal.background = backgroundTexture2;

                 tagsColor = _skin.label.normal.textColor;
                tagsStyle = new GUIStyle(_skin.label);
                tagsStyle.normal.textColor = tagsColor;
                craftNameStyle = new GUIStyle(_skin.label);
                craftNameStyle.normal.textColor = Color.yellow;
                goodLabelStyle = new GUIStyle(_skin.label);
                goodLabelStyle.normal.textColor = Color.green;

                buttonStyleYellow = new GUIStyle(_skin.button);
                buttonStyleYellow.normal.textColor = Color.yellow;
                buttonStyleOrange = new GUIStyle(_skin.button);
                buttonStyleOrange.normal.textColor = HtmlToColor("#fdb915");
                buttonStyleGreen = new GUIStyle(_skin.button);
                buttonStyleGreen.normal.textColor = HtmlToColor("#B7FE00");
            }
        }
    }
}
