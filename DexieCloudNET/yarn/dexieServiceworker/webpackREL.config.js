﻿const path = require('path')
const TerserPlugin = require('terser-webpack-plugin');

module.exports = {
    mode: 'production',
    entry: ['./dexieServiceWorker.ts'],
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: ["ts-loader"],
                exclude: /node_modules/
            },
        ]
    },
    experiments: {
        outputModule: true
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    devtool: 'source-map',
    output: {
        module: true,
        library: {
            type: 'module'
        },
        path: path.resolve(__dirname, '../../wwwroot/js'),
        filename: 'dexieServiceWorker.js',
        publicPath: '_content/DexieCloudNET/js/'
    },
    optimization: {
        minimize: true,
        minimizer: [new TerserPlugin()],
    }
}
