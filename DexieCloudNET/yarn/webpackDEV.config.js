const path = require('path')

module.exports = {
    mode: 'development',
    entry: ['./src/dexieCloudNET.ts'],
    module: {
        rules: [
            {
                test: /\.js$/,
                enforce: "pre",
                use: ["source-map-loader"],
            },
        ]
    },
    experiments: {
        outputModule: true
    },
    devtool: 'source-map',
    output: {
        module: true,
        library: {
            type: 'module'
        },
        path: path.resolve(__dirname, '../wwwroot/js'),
        filename: 'dexieCloudNET.js',
        publicPath: '_content/DexieCloudNET/js/'
    },
    optimization: {
        minimize: false
    }
}
