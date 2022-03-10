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
            .frame(minWidth: 550, idealWidth: 800, maxWidth: nil, minHeight: 320, idealHeight: 450, maxHeight: nil, alignment: .center)
    }
}

struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
    }
}
