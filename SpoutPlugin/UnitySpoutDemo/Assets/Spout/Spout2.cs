﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Spout2 : MonoBehaviour {
	
	private static bool isInit;
	private static bool isReceiving;
	
	public delegate void TextureSharedDelegate(TextureInfo texInfo);
	public static TextureSharedDelegate texSharedDelegate;
	public delegate void SenderStoppedDelegate(TextureInfo texInfo);
	public static SenderStoppedDelegate senderStoppedDelegate;
	
	
	private static List<TextureInfo> newSenders;
	private static List<TextureInfo> stoppedSenders;
	public static List<TextureInfo> activeSenders;

	private static GameObject spoutObject;
	
	public static bool Init()
	{
		return Init (false);
	}
	
	public static bool Init(bool debug)
	{
		if(isInit) return true;	
		
		Debug.Log ("Spout Init !");
		if(debug) initDebugConsole();
		isInit = initNative ();
		
		
		newSenders = new List<TextureInfo>();
		stoppedSenders = new List<TextureInfo>();
		activeSenders = new List<TextureInfo>();
				
		startReceiving ();
		
		spoutObject = new GameObject(); //for automatic update
		spoutObject.AddComponent<Spout2>();
		
		return isInit;
		
		
	}
	
	public static void addListener(TextureSharedDelegate sharedCallback, SenderStoppedDelegate stoppedCallback )
	{
		if(!isInit) Init ();
		Debug.Log ("Add Listener !");
		texSharedDelegate += sharedCallback;
		senderStoppedDelegate += stoppedCallback;
	}	
	
	
	void Update()
	{	

		foreach(TextureInfo s in newSenders)
		{
			if(texSharedDelegate != null) texSharedDelegate(s);
		}
		
		newSenders.Clear();
		
		foreach(TextureInfo s in stoppedSenders)
		{
			if(senderStoppedDelegate != null) senderStoppedDelegate(s);
		}
		
		stoppedSenders.Clear ();
	}
	
	void OnApplicationQuit()
	{
		Debug.Log ("Stop Receiving");
		Spout2.stopReceiving();
	}
	
	public static bool CreateSender(string sharingName, Texture tex)
	{
		if(!isInit) Init();
		return createSenderNative(sharingName, tex.GetNativeTexturePtr());
	}
	
	public static bool UpdateSender(string sharingName, Texture tex)
	{
		return updateSenderNative(sharingName, tex.GetNativeTexturePtr());
	}
	

	public static TextureInfo getTextureInfo (string sharingName)
	{
		foreach(TextureInfo tex in activeSenders)
		{
			if(tex.name == sharingName) return tex;
		}
		
		return null;
	}	
	
	//Imports
	[DllImport ("NativeSpoutPlugin", EntryPoint="init")]
	public static extern bool initNative();
	
	[DllImport ("NativeSpoutPlugin")]
	public static extern void initDebugConsole();
	
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="createSender")]
	private static extern bool createSenderNative (string sharingName, IntPtr texture);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="updateSender")]
	private static extern bool updateSenderNative (string sharingName, IntPtr texture);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="closeSender")]
	public static extern bool CloseSender (string sharingName);
	
	
	//Receiving Thread init
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSenderUpdateDelegate(int numSenders);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSenderStartedDelegate(string senderName, IntPtr resourceView,int textureWidth, int textureHeight);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void SpoutSenderStoppedDelegate(string senderName);
	
	[DllImport ("NativeSpoutPlugin", EntryPoint="startReceiving")]
	private static extern bool startReceivingNative(IntPtr senderUpdateHandler,IntPtr senderStartedHandler,IntPtr senderStoppedHandler);
	[DllImport ("NativeSpoutPlugin")]
	public static extern void stopReceiving();
	
	public static void startReceiving()
	{
		if(isReceiving)return;
		
		SpoutSenderUpdateDelegate senderUpdate_delegate = new SpoutSenderUpdateDelegate(SenderUpdate);
		IntPtr intptr_senderUpdate_delegate = 
			Marshal.GetFunctionPointerForDelegate (senderUpdate_delegate);
		
		SpoutSenderStartedDelegate senderStarted_delegate = new SpoutSenderStartedDelegate(SenderStarted);
		IntPtr intptr_senderStarted_delegate = 
			Marshal.GetFunctionPointerForDelegate (senderStarted_delegate);
		
		SpoutSenderStoppedDelegate senderStopped_delegate = new SpoutSenderStoppedDelegate(SenderStopped);
		IntPtr intptr_senderStopped_delegate = 
			Marshal.GetFunctionPointerForDelegate (senderStopped_delegate);
		
		isReceiving = startReceivingNative(intptr_senderUpdate_delegate, intptr_senderStarted_delegate, intptr_senderStopped_delegate);
	}
	
	public static void SenderUpdate(int numSenders)
	{
		//Debug.Log("Sender update, numSenders : "+numSenders);
	}
	
	public static void SenderStarted(string senderName, IntPtr resourceView,int textureWidth, int textureHeight)
	{
		Debug.Log("Sender started, sender name : "+senderName);
		TextureInfo texInfo = new TextureInfo(senderName);
		texInfo.setInfos(textureWidth,textureHeight,resourceView);
		
		newSenders.Add(texInfo);
		activeSenders.Add (texInfo);
		
		Debug.Log (activeSenders.Count);
	}
	public static void SenderStopped(string senderName)
	{
		Debug.Log("Sender stopped, sender name : "+senderName);
		
		TextureInfo texInfo = new TextureInfo(senderName);
		
		stoppedSenders.Add (texInfo);
		foreach(TextureInfo t in activeSenders)
		{
			if(t.name == texInfo.name)
			{
				activeSenders.Remove(t);
				break;
			}
		}		
		
		Debug.Log ("num sender after delete :"+activeSenders.Count);
	}
	
	
}