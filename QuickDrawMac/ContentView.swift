//
//  ContentView.swift
//  QuickDraw
//
//  Created by Matthew Fraser on 2022-02-28.
//

import SwiftUI
import WebKit

struct ContentView: View {
    var body: some View {
        QuickDrawWebView()
    }
}

struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
    }
}
