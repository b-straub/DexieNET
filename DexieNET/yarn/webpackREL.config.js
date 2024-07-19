const path = require('path')
const TerserPlugin = require('terser-webpack-plugin');
const RemovePlugin = require('remove-files-webpack-plugin');

module.exports = {
    mode: 'production',
    entry: ['./src/dexieNET.ts'],
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
        path: path.resolve(__dirname, '../wwwroot/js'),
        filename: 'dexieNET.js',
        publicPath: '_content/DexieNET/js/'
    },
    optimization: {
        minimize: true,
        minimizer: [new TerserPlugin()],
    },
    plugins: [
        new RemovePlugin({
            before: {
                allowRootAndOutside: true,
                include: [
                    path.resolve(__dirname, '../wwwroot/js')
                ]
            }
        })
    ]
}
