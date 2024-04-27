const path = require('path')
const TerserPlugin = require('terser-webpack-plugin');

module.exports = {
    mode: 'production',
    entry: ['./src/dexieNET.ts'],
    module: {
        rules: [
        ]
    },
    experiments: {
        outputModule: true
    },
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
        minimize: true,
        minimizer: [new TerserPlugin()],
    },
}
