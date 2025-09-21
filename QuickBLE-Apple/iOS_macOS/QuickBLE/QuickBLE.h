//
//  QuickBLE_iOS.h
//  QuickBLE_iOS
//
//  Created by Marcus Behel on 11/22/17.
//

#include <TargetConditionals.h>

#if TARGET_OS_IPHONE
#import <UIKit/UIKit.h>
#else
#import <cocoa/Cocoa.h>
#endif

//! Project version number for QuickBLE_iOS.
FOUNDATION_EXPORT double QuickBLE_VersionNumber;

//! Project version string for QuickBLE_iOS.
FOUNDATION_EXPORT const unsigned char QuickBLE_VersionString[];

// In this header, you should import all the public headers of your framework using statements like #import <QuickBLE/PublicHeader.h>

#import <CoreBluetooth/CoreBluetooth.h>
//#import "QuickBLE-Swift.h"
