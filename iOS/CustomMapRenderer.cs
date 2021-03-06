﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CapstoneProject;
using CapstoneProject.Authentication.Models;
using CapstoneProject.iOS;
using CapstoneProject.ShoppingCart.Models;
using CoreGraphics;
using CoreLocation;
using MapKit;
using ObjCRuntime;
using SQLite.Net.Async;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.iOS;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CustomMap), typeof(CustomMapRenderer))]
namespace CapstoneProject.iOS
{
	public class CustomMapRenderer : MapRenderer
	{
		UIView customPinView;
		List<CustomPin> customPins;
        MKPolylineRenderer polylineRenderer;

		protected override void OnElementChanged(ElementChangedEventArgs<View> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement != null)
			{
				var nativeMap = Control as MKMapView;
				if (nativeMap != null)
				{
					nativeMap.RemoveOverlays(nativeMap.Overlays);
					nativeMap.OverlayRenderer = null;
					polylineRenderer = null;
                    
					nativeMap.RemoveAnnotations(nativeMap.Annotations);
					nativeMap.GetViewForAnnotation = null;
					nativeMap.CalloutAccessoryControlTapped -= OnCalloutAccessoryControlTapped;
					nativeMap.DidSelectAnnotationView -= OnDidSelectAnnotationView;
					nativeMap.DidDeselectAnnotationView -= OnDidDeselectAnnotationView;
				
				}
			}

            //
			MKOverlayRenderer GetOverlayRenderer(MKMapView mapView, IMKOverlay overlayWrapper)
			{
				if (polylineRenderer == null && !Equals(overlayWrapper, null))
				{
					var overlay = Runtime.GetNSObject(overlayWrapper.Handle) as IMKOverlay;
					polylineRenderer = new MKPolylineRenderer(overlay as MKPolyline)
					{
						FillColor = UIColor.Blue,
						StrokeColor = UIColor.Red,
						LineWidth = 3,
						Alpha = 0.4f
					};
				}
				return polylineRenderer;
			}
            //

			if (e.NewElement != null)
			{
				var formsMap = (CustomMap)e.NewElement;
				var nativeMap = Control as MKMapView;
                nativeMap.OverlayRenderer = GetOverlayRenderer;

				CLLocationCoordinate2D[] coords = new CLLocationCoordinate2D[formsMap.RouteCoordinates.Count];
				int index = 0;
				foreach (var position in formsMap.RouteCoordinates)
				{
					coords[index] = new CLLocationCoordinate2D(position.Latitude, position.Longitude);
					index++;
				}
				var routeOverlay = MKPolyline.FromCoordinates(coords);
				nativeMap.AddOverlay(routeOverlay);

				customPins = formsMap.CustomPins;
				nativeMap.GetViewForAnnotation = GetViewForAnnotation;
				nativeMap.CalloutAccessoryControlTapped += OnCalloutAccessoryControlTapped;
				nativeMap.DidSelectAnnotationView += OnDidSelectAnnotationView;
				nativeMap.DidDeselectAnnotationView += OnDidDeselectAnnotationView;

			}
		}

		MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
		{
			MKAnnotationView annotationView = null;

			if (annotation is MKUserLocation)
				return null;

			var anno = annotation as MKPointAnnotation;
			var customPin = GetCustomPin(anno);
			if (customPin == null)
			{
				throw new Exception("Custom pin not found");
			}

			annotationView = mapView.DequeueReusableAnnotation(customPin.Id);
			if (annotationView == null)
			{
				annotationView = new CustomMKAnnotationView(annotation, customPin.Id)
				{
					Image = UIImage.FromFile("pin1.png"),
					CalloutOffset = new CGPoint(0, 0),
					//LeftCalloutAccessoryView = new UIImageView(UIImage.FromFile("kiwibuy_logo.png")),
					RightCalloutAccessoryView = UIButton.FromType(UIButtonType.DetailDisclosure)
				};
				((CustomMKAnnotationView)annotationView).Id = customPin.Id;
				((CustomMKAnnotationView)annotationView).Url = customPin.Url;
			}
			annotationView.CanShowCallout = true;
			return annotationView;
		}

		void OnCalloutAccessoryControlTapped(object sender, MKMapViewAccessoryTappedEventArgs e)
		{
			var customView = e.View as CustomMKAnnotationView;
			if (!string.IsNullOrWhiteSpace(customView.Url))
			{
				UIApplication.SharedApplication.OpenUrl(new Foundation.NSUrl(customView.Url));
			}
		}

		void OnDidSelectAnnotationView(object sender, MKAnnotationViewEventArgs e)
		{
			var customView = e.View as CustomMKAnnotationView;
			customPinView = new UIView();



			//if (customView.Id == "Kiwibuy")
			//{
				//customPinView.Frame = new CGRect(0, 0, 200, 84);
				//var image = new UIImageView(new CGRect(0, 0, 200, 84));
				//image.Image = UIImage.FromFile("kiwibuy_logo_big.png");
				//customPinView.AddSubview(image);
				//customPinView.Center = new CGPoint(0, -(e.View.Frame.Height + 75));
				//e.View.AddSubview(customPinView);
            //}

		}

		void OnDidDeselectAnnotationView(object sender, MKAnnotationViewEventArgs e)
		{
			if (!e.View.Selected)
			{
				customPinView.RemoveFromSuperview();
				customPinView.Dispose();
				customPinView = null;
			}
		}

		CustomPin GetCustomPin(MKPointAnnotation annotation)
		{
			var position = new Position(annotation.Coordinate.Latitude, annotation.Coordinate.Longitude);
			return customPins.FirstOrDefault(pin => pin.Pin.Position == position);
		}
	}
}
