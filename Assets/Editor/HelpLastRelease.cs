﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class HelpLastRelease : EditorWindow {

	#region Urls

	const string experimenalUrl = @"http://unity3d.com/experimental";
	const string roadmapUrl = @"http://unity3d.com/unity/roadmap";
	const string statusCloudUrl = @"http://status.cloud.unity3d.com";

	const string archiveUrl = @"http://unity3d.com/get-unity/download/archive";
	const string betaArchiveUrl = @"http://unity3d.com/unity/beta/archive";
	const string ltsArchiveUrl = @"http://unity3d.com/unity/qa/lts-releases";
	const string releaseUrlBeta = @"http://beta.unity3d.com/download/{0}/{1}";

	const string searchUrl = @"http://unity3d.com/search";
	const string searchGoogleUrl = @"http://www.google.com/cse/publicurl?cx=000020748284628035790:axpeo4rho5e";
	const string searchGitHubUrl = @"http://unitylist.com";
	const string searchIssueUrl = @"http://issuetracker.unity3d.com";

	const string assistantUrl = @"http://beta.unity3d.com/download/{0}/UnityDownloadAssistant-{1}.{2}";
	const string torrentUrl = @"http://download.unity3d.com/download_unity/{0}/Unity.torrent";
	const string serverUrl = @"http://symbolserver.unity3d.com/";
	const string historyUrl = serverUrl + @"000Admin/history.txt";
	const string iniUrl = @"http://beta.unity3d.com/download/{0}/unity-{1}-{2}.ini";

	const string finalRN = @"http://unity3d.com/unity/whats-new/unity-";
	const string ltsRN = @"http://unity3d.com/unity/whatsnew/unity-";
	const string betaRN = @"http://unity3d.com/unity/beta/unity";
	const string patchRN = @"http://unity3d.com/unity/qa/patch-releases/";

	const string tutorialsUrl = @"http://unity3d.com/learn/tutorials";
	const string knowledgeBaseUrl = @"http://support.unity3d.com";
	const string customerServiceUrl = @"http://support.unity3d.com/hc/en-us/requests/new?ticket_form_id=65905";
	const string liveTrainingUrl = @"http://unity3d.com/learn/live-training";
	const string faqUrl = @"https://unity3d.com/unity/faq";

	const string githubUTUrl = @"http://github.com/Unity-Technologies";
	const string bitbucketUTUrl = @"http://bitbucket.org/Unity-Technologies";
	const string newsUrl = @"http://unity_news.tggram.com";

	const string githubUrl = @"http://api.github.com/repos/dmbond/CheckVersion/releases/latest";

	#endregion

	#region Vars

	#pragma warning disable 0649, 1635

	[Serializable]
	class GithubRelease {
		public string created_at;
		public GithubAsset[] assets;
	}

	static GithubRelease release = null;

	[Serializable]
	class GithubAsset {
		public string browser_download_url;
	}

	#pragma warning restore 0649, 1635

	static readonly string zipName = Application.platform == RuntimePlatform.WindowsEditor ? "7z" : "7za";
	const string baseName = "UnityYAMLMerge.ex";
	const string compressedName = baseName + "_";
	const string extractedName = baseName + "e";
	static string tempDir;

	static WWW wwwHistory, wwwList, wwwMerger, wwwAssistant;
	static WWW wwwGithub, wwwPackage;
	static WWW wwwIniWin, wwwIniOSX, wwwIniLinux;
	static WWW wwwReleaseNotes, wwwTorrent;

	static SortedList<string, string> fullList, sortedList, currentList;

	static int idxSelectedInCurrent = -1;
	static string selectedVersion;
	static string selectedRevision;
	static Action ReleaseCallback;

	static int idxOS = -1;
	static readonly string[] titlesOS = { "Win", "OSX" };
	static readonly string[] titlesOSLinux = { "Win", "OSX", "Linux" };
	static Dictionary<string, Dictionary<string, string>> dictIniWin, dictIniOSX, dictIniLinux;
	static bool hasLinux, hasReleaseNotes, hasTorrent;

	static HelpLastRelease window;
	static string wndTitle;
	const string scriptName = "HelpLastRelease";
	const string prefs = scriptName + ".";
	const string prefsCount = prefs + "count";
	static bool hasUpdate = false;

	static string filterString = "";
	static string universalDT = "yyyy-MM-ddTHH:mm:ssZ";
	static string nullDT = "1970-01-01T00:00:00Z";
	static string srcDT = "MM/dd/yyyy HH:mm:ss";
	static string listDT = "dd-MM-yyyy";
	static GUIStyle btnStyle;
	static Vector2 scrollPos;

	const string rnTooltip = "Open Release Notes";
	const string torrentTooltip = "Open Torrent";
	const string assistTooltip = "Open Download Assistant";
	const string versionTooltip = "Open Download Page";
	const string infoTooltip = "Show more info";
	const string updateTooltip = "Update from Github";

	static readonly Dictionary<string, Color> colors = new Dictionary<string, Color>() {
		{ "2017.1.", Color.blue },
		{ "2017.2.", Color.cyan },
		{ "2017.3.", Color.magenta },
		{ "2017.4.", Color.green },
		{ "2018.1.", Color.yellow },
		{ "2018.2.", Color.red },
		{ "2018.3.", Color.red },
		{ "2018.4.", Color.red }
	};
	static Color oldColor = Color.white;
	static Color currentColor = Color.black;
	static float alphaBackForPersonal = 0.3f;
	static Color alpha = new Color(1f, 1f, 1f, alphaBackForPersonal);

	#endregion

	#region Menu

	[MenuItem("Help/Links/Releases...", false, 010)]
	static void Init() {
		window = GetWindow<HelpLastRelease>(wndTitle);
		SortList(String.Empty);
	}

	[MenuItem("Help/Links/Check for Updates...", false, 015)]
	static void CheckforUpdates() {
		window = GetWindow<HelpLastRelease>(wndTitle);
		int index = Application.unityVersion.LastIndexOf('.');
		string filter = Application.unityVersion.Substring(0, index + 1);
		SortList(filter);
	}
	// ---

	[MenuItem("Help/Links/Search...", false, 100)]
	static void OpenSearch() {
		Application.OpenURL(searchUrl);
	}

	[MenuItem("Help/Links/Search Google...", false, 100)]
	static void OpenSearchGoogle() {
		Application.OpenURL(searchGoogleUrl);
	}

	[MenuItem("Help/Links/Search GitHub...", false, 105)]
	static void OpenSearchGitHub() {
		Application.OpenURL(searchGitHubUrl);
	}

	[MenuItem("Help/Links/Search Issue...", false, 110)]
	static void OpenSearchIssue() {
		Application.OpenURL(searchIssueUrl);
	}
	// ---	

	[MenuItem("Help/Links/Archive...", false, 200)]
	static void OpenArchive() {
		Application.OpenURL(archiveUrl);
	}

	[MenuItem("Help/Links/LTS Archive...", false, 205)]
	static void OpenLTSArchive() {
		Application.OpenURL(ltsArchiveUrl);
	}

	[MenuItem("Help/Links/Beta Archive...", false, 205)]
	static void OpenBetaArchive() {
		Application.OpenURL(betaArchiveUrl);
	}

	[MenuItem("Help/Links/Patch Archive...", false, 210)]
	static void OpenPatchArchive() {
		Application.OpenURL(patchRN);
	}
	// ---

	[MenuItem("Help/Links/Tutorials...", false, 700)]
	static void OpenBestPractices() {
		Application.OpenURL(tutorialsUrl);
	}

	[MenuItem("Help/Links/Knowledge Base...", false, 705)]
	static void OpenKnowledgeBase() {
		Application.OpenURL(knowledgeBaseUrl);
	}

	[MenuItem("Help/Links/Customer Service...", false, 707)]
	static void OpenCustomerService() {
		Application.OpenURL(customerServiceUrl);
	}

	[MenuItem("Help/Links/Live Training...", false, 710)]
	static void OpenLiveTraining() {
		Application.OpenURL(liveTrainingUrl);
	}

	[MenuItem("Help/Links/FAQ...", false, 715)]
	static void OpenFaq() {
		Application.OpenURL(faqUrl);
	}
	// ---

	[MenuItem("Help/Links/Roadmap...", false, 800)]
	static void OpenRoadmap() {
		Application.OpenURL(roadmapUrl);
	}

	[MenuItem("Help/Links/Experimental...", false, 805)]
	static void OpenExperimental() {
		Application.OpenURL(experimenalUrl);
	}

	[MenuItem("Help/Links/Status Cloud...", false, 810)]
	static void OpenStatusCloud() {
		Application.OpenURL(statusCloudUrl);
	}
	// ---

	[MenuItem("Help/Links/Github UT...", false, 830)]
	static void OpenGithubUT() {
		Application.OpenURL(githubUTUrl);
	}

	[MenuItem("Help/Links/Bitbucket UT...", false, 835)]
	static void OpenBitbucketUT() {
		Application.OpenURL(bitbucketUTUrl);
	}

	[MenuItem("Help/Links/News...", false, 840)]
	static void OpenNews() {
		Application.OpenURL(newsUrl);
	}

	#endregion

	#region GUI

	void OnGUI() {
		if (fullList != null) {
			GUILayout.BeginHorizontal();
			ListGUI();
			InfoGUI();
			GUILayout.EndHorizontal();
		} else {
			WaitGUI();
		}
	}

	void ListGUI() {
		btnStyle = new GUIStyle(EditorStyles.miniButton);
		GUILayout.BeginVertical(GUILayout.Width(210));
		SearchVersionGUI();
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
		if (currentList == null) currentList = fullList;
		for (int i = currentList.Count - 1; i >= 0; i--) {
			GUILayout.BeginHorizontal();
			ColorGUI(i);
			btnStyle.alignment = TextAnchor.MiddleCenter;
			#if UNITY_5_5_OR_NEWER
			if (Application.platform != RuntimePlatform.LinuxEditor)			
			#endif
			if (GUILayout.Button(new GUIContent("A", assistTooltip), btnStyle)) {
				DownloadList(i, DownloadAssistant);
			}
			btnStyle.alignment = TextAnchor.MiddleLeft;
			if (GUILayout.Button(new GUIContent(currentList.Values[i], infoTooltip), btnStyle, GUILayout.MinWidth(160f))) {
				DownloadList(i, UpdateInfo);
			}
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
		UpdateGUI();
		GUILayout.EndVertical();
	}

	void UpdateGUI() {
		if (hasUpdate) {
			GUILayout.Space(5f);
			if (EditorGUIUtility.isProSkin) {
				GUI.contentColor = Color.green;
			} else {
				GUI.backgroundColor = Color.green * alpha;
			}
			btnStyle.alignment = TextAnchor.MiddleCenter;
			if (GUILayout.Button(new GUIContent("Update is available", updateTooltip), btnStyle)) {
				if (release != null) {
					hasUpdate = false;
					DownloadPackage(release.assets[0].browser_download_url);
				}
			}
			GUILayout.Space(5f);
		}
	}

	void InfoGUI() {
		if (idxSelectedInCurrent == -1) return;
		if (EditorGUIUtility.isProSkin) {
			GUI.contentColor = oldColor;
		} else {
			GUI.backgroundColor = oldColor * alpha;
		}
		GUILayout.BeginVertical(GUILayout.Width(390));
		GUILayout.Space(5f);
		GUILayout.BeginHorizontal();
		btnStyle.alignment = TextAnchor.MiddleCenter;
		if (!string.IsNullOrEmpty(selectedRevision) && GUILayout.Button(new GUIContent(string.Format("{0} ({1})", selectedVersion, selectedRevision), versionTooltip), btnStyle)) {
			Application.OpenURL(string.Format(releaseUrlBeta, selectedRevision, "download.html"));
		}
		if (hasReleaseNotes && GUILayout.Button(
			new GUIContent("Release Notes", rnTooltip), btnStyle)) {
			Application.OpenURL(wwwReleaseNotes.url);
		}
		if (hasTorrent && GUILayout.Button(
			new GUIContent("Torrent", torrentTooltip), btnStyle)) {
			StartTorrent();
		}
		GUILayout.EndHorizontal();
		Dictionary<string, Dictionary<string, string>> dict = null;
		if (!string.IsNullOrEmpty(selectedRevision)) {
			GUILayout.BeginHorizontal();
			idxOS = GUILayout.SelectionGrid(idxOS, hasLinux ? titlesOSLinux : titlesOS, hasLinux ? 3 : 2,
				btnStyle);
			switch (idxOS) {
				case 0:
					dict = dictIniWin;
					break;
				case 1:
					dict = dictIniOSX;
					break;
				case 2:
					dict = dictIniLinux;
					break;
			}
			GUILayout.EndHorizontal();
		}
		if (dict != null) {
			GUILayout.BeginVertical();
			GUILayout.Space(5f);
			btnStyle.alignment = TextAnchor.MiddleLeft;
			foreach (var key in dict.Keys) {
				if (GUILayout.Button(new GUIContent(dict[key]["title"], dict[key]["description"]), btnStyle)) {
					var url = dict[key]["url"].StartsWith("http") ? dict[key]["url"] :
						string.Format(releaseUrlBeta, selectedRevision, dict[key]["url"]);
					EditorGUIUtility.systemCopyBuffer = url;
					ShowNotification(new GUIContent("URL copied to the clipboard"));
				}
			}
			GUILayout.EndVertical();
		}
		GUILayout.EndVertical();
	}

	void ColorGUI(int i) {
		foreach (var k in colors.Keys) {
			bool isColored = currentList.Values[i].Contains(k);
			if (EditorGUIUtility.isProSkin) {
				GUI.contentColor = isColored ? colors[k] : oldColor;
			} else {
				GUI.backgroundColor = isColored ? colors[k] * alpha : oldColor * alpha;
			}
			if (isColored) break;
		}
	}

	static void SearchVersionGUI() {
		string s = string.Empty;
		GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
		s = GUILayout.TextField(filterString, GUI.skin.FindStyle("ToolbarSeachTextField"));
		if (GUILayout.Button(string.Empty, GUI.skin.FindStyle("ToolbarSeachCancelButton"))) {
			s = string.Empty;
			GUI.FocusControl(null);
		}
		GUILayout.EndHorizontal();
		if (s != filterString) {
			SortList(s);
		}
	}

	static void FillMenu(WWW history) {
		fullList = new SortedList<string, string>();
		string build;
		//0000000001,add,file,02/03/2015,13:13:44,"Unity","5.0.0b22","",
		string[] parts, releases = history.text.Split('\n');
		for (int i = 0; i < releases.Length; i++) {
			parts = releases[i].Split(',');
			DateTime dt = DateTime.ParseExact(string.Format("{0} {1}", parts[3], parts[4]), srcDT, CultureInfo.InvariantCulture);
			build = string.Format("{0} ({1})", parts[6].Trim('\"'), dt.ToString(listDT));
			fullList.Add(parts[0], build);
		}
		CheckNewVersion();
		if (!string.IsNullOrEmpty(filterString)) SortList(filterString);
		if (window == null) {
			HelpLastRelease[] w = Resources.FindObjectsOfTypeAll<HelpLastRelease>();
			if (w != null && w.Length > 0) window = w[0];
		}
		if (window != null) window.Repaint();
	}

	static void SortList(string filter) {
		if (!string.IsNullOrEmpty(filter) && fullList != null) {
			sortedList = new SortedList<string, string>();
			for (int i = fullList.Count - 1; i >= 0; i--) {
				if (fullList.Values[i].Contains(filter)) {
					sortedList.Add(fullList.Keys[i], fullList.Values[i]);
				}
			}
			currentList = sortedList;
		} else currentList = fullList;
		filterString = filter;
	}

	static void ProgressGUI(WWW www, string text) {
		if (www != null && !www.isDone && string.IsNullOrEmpty(www.error)) {
			EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), www.progress,
				string.IsNullOrEmpty(www.error) ?
					string.Format("{0} ({1}%) {2} kB",
						text,
						Mathf.RoundToInt(www.progress * 100f),
						www.bytesDownloaded / 1024) :
					string.Format("{0} ({1}%) {2} kB",
						www.error,
						Mathf.RoundToInt(www.progress * 100f))
			);
		}
	}

	void WaitGUI() {
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Wait...");
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
	}

	#endregion

	#region Window

	void OnEnable() {
		tempDir = SetTempDir();
		DownloadHistory();
	}

	[InitializeOnLoadMethod]
	static void AutoUpdate() {
		wndTitle = string.Format("v {0}", Application.unityVersion);
		colors.Add(Application.unityVersion, currentColor);
		if (Application.internetReachability != NetworkReachability.NotReachable) {
			DownloadGithub();
		}
	}

	#endregion

	#region Download

	static void UpdateInfo() {
		idxOS = Application.platform == RuntimePlatform.WindowsEditor ? 0 : 1;
		window.Repaint();
		if (!string.IsNullOrEmpty(selectedRevision)) {
			DownloadIniWin(selectedRevision, selectedVersion);
			DownloadIniOSX(selectedRevision, selectedVersion);
			DownloadIniLinux(selectedRevision, selectedVersion);
			DownloadTorrent(selectedRevision, selectedVersion);
		}
	}

	static void DownloadAssistant() {
		UpdateInfo();
		string ext = Application.platform == RuntimePlatform.WindowsEditor ? "exe" : "dmg";
		string url = string.Format(assistantUrl, selectedRevision, selectedVersion, ext);
		wwwAssistant = new WWW(url);
		EditorApplication.update += WaitAssistant;
	}

	static void DownloadHistory() {
		wwwHistory = new WWW(historyUrl);
		EditorApplication.update += WaitHistory;
	}

	static void DownloadList(int historyNum, Action callback) {
		hasTorrent = false;
		hasReleaseNotes = false;
		idxSelectedInCurrent = historyNum;
		selectedVersion = currentList.Values[idxSelectedInCurrent].Split(' ')[0];
		DownloadReleaseNotes(VersionToReleaseNotesUrl(selectedVersion));
		selectedRevision = EditorPrefs.GetString(prefs + currentList.Keys[idxSelectedInCurrent], "");
		if (!string.IsNullOrEmpty(selectedRevision)) {
			if (callback != null) callback();
		} else {
			ReleaseCallback = callback;
			string listUrl = string.Format("{0}000Admin/{1}", serverUrl, currentList.Keys[idxSelectedInCurrent]);
			wwwList = new WWW(listUrl);
			EditorApplication.update += WaitList;
		}
	}

	static string VersionToReleaseNotesUrl(string version, bool repeat = false) {
		string url = null;
		string versionDigits;
		if (version.Contains("a")) {
			url = betaRN;
		}
		if (version.Contains("p")) {
			versionDigits = version.Split(' ')[0];
			url = patchRN + versionDigits;
		}
		if (version.Contains("f")) {
			versionDigits = version.Split('f')[0];
			// RC
			if (versionDigits.EndsWith("0")) {
				// old releases
				if (versionDigits.StartsWith("5.3") || versionDigits.StartsWith("5.2") ||
				    versionDigits.StartsWith("5.1") || versionDigits.StartsWith("5.0")) {
					url = finalRN + versionDigits.Substring(0, 3);
				} else {
					if (repeat == false) {
						url = betaRN + version;
					} else {
						url = finalRN + versionDigits;
					}
				}
			} else {
				// LTS or new Final
				if ((versionDigits.Contains(".4.") && versionDigits.Length > 7) || repeat) {
					url = ltsRN + versionDigits;
				} else {
					url = finalRN + versionDigits;
				}
			}
		}
		if (version.Contains("b")) {
			versionDigits = version.Split(' ')[0];
			url = betaRN + versionDigits;
		}
		return url;
	}

	static void DownloadReleaseNotes(string url) {
		hasReleaseNotes = false;
		wwwReleaseNotes = new WWW(url);
		EditorApplication.update += WaitReleaseNotes;
	}

	static void DownloadTorrent(string revision, string version) {
		hasTorrent = false;
		if (version.Contains("f")) {
			string url = String.Format(torrentUrl, revision);
			wwwTorrent = new WWW(url);
			EditorApplication.update += WaitTorrent;
		}
	}

	static void DownloadIniWin(string revision, string version) {
		dictIniWin = null;
		string url = String.Format(iniUrl, revision, version, "win");
		wwwIniWin = new WWW(url);
		EditorApplication.update += WaitIniWin;
	}

	static void DownloadIniOSX(string revision, string version) {
		dictIniOSX = null;
		string url = String.Format(iniUrl, revision, version, "osx");
		wwwIniOSX = new WWW(url);
		EditorApplication.update += WaitIniOSX;
	}

	static void DownloadIniLinux(string revision, string version) {
		dictIniLinux = null;
		hasLinux = false;
		string url = String.Format(iniUrl, revision, version, "linux");
		wwwIniLinux = new WWW(url);
		EditorApplication.update += WaitIniLinux;
	}

	static void DownloadMerger(string mergerUrl) {
		wwwMerger = new WWW(mergerUrl);
		EditorApplication.update += WaitMerger;
	}

	static void DownloadPackage(string packageUrl) {
		wwwPackage = new WWW(packageUrl);
		EditorApplication.update += WaitPackage;
	}

	static void DownloadGithub() {
		wwwGithub = new WWW(githubUrl);
		EditorApplication.update += WaitGithub;
	}

	#endregion

	#region Wait

	static void WaitList() {
		Wait(wwwList, WaitList, ParseList);
	}

	static void WaitHistory() {
		Wait(wwwHistory, WaitHistory, FillMenu);
	}

	static void WaitAssistant() {
		Wait(wwwAssistant, WaitAssistant, SaveAssistant);
	}

	static void WaitReleaseNotes() {
		Wait(wwwReleaseNotes, WaitReleaseNotes, ParseReleaseNotes);
	}

	static void WaitTorrent() {
		Wait(wwwTorrent, WaitTorrent, ProcessTorrent);
	}

	static void WaitIniWin() {
		Wait(wwwIniWin, WaitIniWin, ParseIniWin);
	}

	static void WaitIniOSX() {
		Wait(wwwIniOSX, WaitIniOSX, ParseIniOSX);
	}

	static void WaitIniLinux() {
		Wait(wwwIniLinux, WaitIniLinux, ParseIniLinux);
	}

	static void WaitMerger() {
		Wait(wwwMerger, WaitMerger, SaveMerger);
	}

	static void WaitGithub() {
		Wait(wwwGithub, WaitGithub, ParseGithub);
	}

	static void WaitPackage() {
		Wait(wwwPackage, WaitPackage, ImportPackage);
	}

	static void Wait(WWW www, EditorApplication.CallbackFunction caller, Action<WWW> action) {
		if (www != null && www.isDone) {
			EditorApplication.update -= caller;
			if (string.IsNullOrEmpty(www.error) && www.bytesDownloaded > 0) {
				//Debug.LogFormat("{0} kB: {1}", www.size/1024, www.url);
				if (action != null) action(www);
			} //else Debug.LogWarningFormat("{0} {1}", www.url, www.error);
		}
	}

	#endregion

	#region Actions after download

	static void SaveAssistant(WWW assistant) {
		if (!Directory.Exists(tempDir)) {
			Directory.CreateDirectory(tempDir);
		}
		string name = Path.GetFileName(assistant.url);
		string path = Path.Combine(tempDir, name);
		File.WriteAllBytes(path, assistant.bytes);
		if (Application.platform == RuntimePlatform.WindowsEditor) {
			Application.OpenURL(path);
		} else {
			StartAssistant(path);
		}
	}

	static void StartAssistant(string path) {
		string cmd = "hdiutil";
		string arg = string.Format("mount '{0}'", path);
		try {
			using (Process assist = new Process()) {
				assist.StartInfo.FileName = cmd;
				assist.StartInfo.Arguments = arg;
				assist.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
				assist.StartInfo.CreateNoWindow = true;
				assist.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				assist.Start();
			}
		} catch (Exception e) {
			Debug.LogErrorFormat("{0} {1}\n{2}", cmd, arg, e.Message);
		}
	}

	static void ParseList(WWW list) {
		string[] files = list.text.Split('\n');
		string[] parts;
		string mergerUrl = null;
		for (int i = 0; i < files.Length; i++) {
			parts = files[i].Split(',');
			if (parts[0].Contains(extractedName)) {
				mergerUrl = string.Format("{0}{1}/{2}", serverUrl, parts[0].Trim('\"').Replace('\\', '/'), compressedName);
				DownloadMerger(mergerUrl);
				break;
			}
		}
		if (string.IsNullOrEmpty(mergerUrl) && ReleaseCallback != null) ReleaseCallback();
	}

	static void ParseIniWin(WWW ini) {
		ParseIni(ini, out dictIniWin);
	}

	static void ParseIniOSX(WWW ini) {
		ParseIni(ini, out dictIniOSX);
	}

	static void ParseIniLinux(WWW ini) {
		hasLinux = wwwIniLinux != null && string.IsNullOrEmpty(wwwIniLinux.error);
		if (hasLinux) {
			ParseIni(ini, out dictIniLinux);
			#if UNITY_5_5_OR_NEWER
			if (Application.platform == RuntimePlatform.LinuxEditor) {
				idxOS = 2;
				window.Repaint();
			}
			#endif
		}
	}

	static void ParseIni(WWW ini, out Dictionary<string, Dictionary<string, string>> dictIni) {
		string[] lines = ini.text.Split('\n');
		string section = null;
		Dictionary<string, string> dict = null;
		dictIni = new Dictionary<string, Dictionary<string, string>>();
		for (int i = 0; i < lines.Length; i++) {
			string line = lines[i].Trim();
			if (string.IsNullOrEmpty(line)) continue;
			if (line.StartsWith("[") && line.EndsWith("]")) {
				if (section != null && dict != null) {
					dictIni.Add(section, dict);
				}
				section = line.Substring(1, line.Length - 2);
				dict = new Dictionary<string, string>();
			} else {
				var parts = line.Split('=');
				if (parts.Length > 1) {
					dict.Add(parts[0].Trim(), parts[1].Trim());
				}
			}
		}
		if (section != null && dict != null) {
			dictIni.Add(section, dict);
		}
		window.Repaint();
	}

	static void ParseReleaseNotes(WWW www) {
		bool err403 = www.text.Contains("403</h1>");
		bool err404 = www.text.Contains("404</h1>");
		if (!string.IsNullOrEmpty(www.error) || err403 || err404) {
			if (selectedVersion.Contains("f")) {
				string url = VersionToReleaseNotesUrl(selectedVersion, true);
				if (url != www.url) {
					DownloadReleaseNotes(url);
				} else {
					wwwReleaseNotes = null;
				}
			} else {
				wwwReleaseNotes = null;
			}
		}
		hasReleaseNotes = wwwReleaseNotes != null && string.IsNullOrEmpty(wwwReleaseNotes.error) && !err403 && !err404;
		if (hasReleaseNotes) {
			window.Repaint();
			int idx = www.text.IndexOf("Revision: ");
			if (idx != -1 && string.IsNullOrEmpty(selectedRevision)) {
				selectedRevision = www.text.Substring(idx + 10, 12);
				EditorPrefs.SetString(prefs + currentList.Keys[idxSelectedInCurrent], selectedRevision);
				UpdateInfo();
				window.Repaint();
			}
		}
	}

	static void ProcessTorrent(WWW www) {
		if (!string.IsNullOrEmpty(www.error) || www.text.Contains("403</h1>") || www.text.Contains("404</h1>")) {
			wwwTorrent = null;
		}
		hasTorrent = wwwTorrent != null && string.IsNullOrEmpty(wwwTorrent.error);
		if (hasTorrent) {
			SaveTorrent(wwwTorrent);
			window.Repaint();
		}
	}

	static void SaveTorrent(WWW torrent) {
		if (!Directory.Exists(tempDir)) {
			Directory.CreateDirectory(tempDir);
		}
		string path = Path.Combine(tempDir, "Unity.torrent");
		File.WriteAllBytes(path, torrent.bytes);
	}

	static void StartTorrent() {
		string path = Path.Combine(tempDir, "Unity.torrent");
		if (File.Exists(path)) {
			Application.OpenURL(path);
		} else {
			Application.OpenURL(wwwTorrent.url);
		}
	}

	static void SaveMerger(WWW merger) {
		if (!Directory.Exists(tempDir)) {
			Directory.CreateDirectory(tempDir);
		}
		string path = Path.Combine(tempDir, compressedName);
		File.WriteAllBytes(path, merger.bytes);
		ExtractMerger(path);
	}

	static void ExtractMerger(string path) {
		string zipPath = string.Format("{0}/Tools/{1}", EditorApplication.applicationContentsPath, zipName);
		string arg = string.Format("e -y \"{0}\"", path);
		try {
			using (Process unzip = new Process()) {
				unzip.StartInfo.FileName = zipPath;
				unzip.StartInfo.Arguments = arg;
				unzip.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);
				unzip.StartInfo.CreateNoWindow = true;
				unzip.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				unzip.Start();
				unzip.WaitForExit();
				SearchVersion();
			}
		} catch (Exception e) {
			Debug.LogErrorFormat("{0} {1}\n{2}", zipPath, arg, e.Message);
		}
	}

	static void ParseGithub(WWW github) {
		release = JsonUtility.FromJson<GithubRelease>(github.text);
		string current = EditorPrefs.GetString(prefs + Application.productName, nullDT);
		if (DateTime.ParseExact(release.created_at, universalDT, CultureInfo.InvariantCulture) > 
		    DateTime.ParseExact(current, universalDT, CultureInfo.InvariantCulture)) {
			hasUpdate = true;
			if (window != null) window.Repaint();
		}
	}

	static void ImportPackage(WWW package) {
		tempDir = SetTempDir();
		string name = string.Format("{0}.unitypackage", scriptName);
		string path = Path.Combine(tempDir, name);
		File.WriteAllBytes(path, package.bytes);
		AssetDatabase.ImportPackage(path, false);
		EditorPrefs.SetString(prefs + Application.productName, release.created_at);
		Debug.LogFormat("{0} updated from Github", scriptName);
	}

	static string SetTempDir() {
		string result = string.Format("{0}/../Temp/{1}", Application.dataPath, scriptName);
		if (!Directory.Exists(result)) {
			Directory.CreateDirectory(result);
		}
		return result;
	}

	static void CheckNewVersion() {
		int count = EditorPrefs.GetInt(prefsCount, 0);
		if (fullList.Count > count) {
			EditorApplication.Beep();
			string color = EditorGUIUtility.isProSkin ? "yellow" : "red";
			Debug.LogFormat("New version: <color={0}>{1}</color>", color,
				fullList.Values[fullList.Count - 1]);
			EditorPrefs.SetInt(prefsCount, fullList.Count);
			currentList = null;
		}
	}

	static void SearchVersion() {
		string path = Path.Combine(tempDir, extractedName);
		if (File.Exists(path)) {
			string[] lines;
			lines = File.ReadAllLines(path, Encoding.Unicode);
			FileUtil.DeleteFileOrDirectory(Path.GetDirectoryName(path));
			string version = currentList.Values[idxSelectedInCurrent].Split(' ')[0] + "_";
			for (int i = 0; i < lines.Length; i++) {
				if (lines[i].Contains(version)) {
					int pos = lines[i].IndexOf(version);
					selectedRevision = lines[i].Substring(pos + version.Length, 12);
					EditorPrefs.SetString(prefs + currentList.Keys[idxSelectedInCurrent], selectedRevision);
					if (ReleaseCallback != null) ReleaseCallback();
					window.Repaint();
					break;
				}
			}
		}
	}

	#endregion

}
