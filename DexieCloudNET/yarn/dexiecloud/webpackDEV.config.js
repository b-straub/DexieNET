const path = require('path')

module.exports = {
    mode: 'development',
    entry: ['./dexieCloudNET.ts'],
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
        filename: 'dexieCloudNET.js',
        publicPath: '_content/DexieCloudNET/js/'
    },
    optimization: {
        minimize: false
    }
}
