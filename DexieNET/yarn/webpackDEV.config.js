const path = require('path')

module.exports = {
    mode: 'development',
    entry: ['./src/dexieNET.ts'],
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
        filename: 'dexieNET.js',
        publicPath: '_content/DexieNET/js/'
    },
    optimization: {
        minimize: false
    }
}
