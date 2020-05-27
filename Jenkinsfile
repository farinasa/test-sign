pipeline{
    agent{
        kubernetes {
            label "codegentest-${UUID.randomUUID().toString()}" 
            yamlFile 'build/build-spec.yaml'
            defaultContainer 'build'
        }
    }

    environment{
        IMAGE_NAME = "codegentest"
        AWS_DEFAULT_REGION = "us-east-1"
        AWS_CREDENTIALS = credentials("")
    }

    parameters {
        choice(name: 'CAKE_LOG_LEVEL', choices: ['Normal', 'Quiet', 'Minimal', 'Verbose', 'Diagnostic'], description: 'Cake logging level')
    }

    stages{
        stage("Prepare") {
            steps {
                sh "dotnet tool restore"
                sh "dotnet cake ./build/build.cake --bootstrap --verbosity=${params.CAKE_LOG_LEVEL}"
            }
        }

        stage("Get Version") {
            environment{
                LD_LIBRARY_PATH="/root/.nuget/packages/gitversion.tool/5.1.2/tools/netcoreapp3.0/any/runtimes/debian.9-x64/native/"
                // Required to enable Jenkins PR builds to run step without error
                IGNORE_NORMALISATION_GIT_HEAD_MOVE = "1"
            }
            steps {
                sh "dotnet cake ./build/build.cake --target=GetVersion --exclusive --verbosity=${params.CAKE_LOG_LEVEL}"
                script {
                    // uses the Jenkins plugin Pipeline Utility Steps to parse the json file
                    env.APP_VERSION = readJSON(file: 'build/version.json').SemVer
                }
            }
        }

        stage("Build"){
            steps {
                sh "dotnet cake ./build/build.cake --target=Build --app-version=${APP_VERSION} --exclusive --verbosity=${params.CAKE_LOG_LEVEL}"
            }
        }

        stage("Tests") {
            parallel {
                stage ("Unit Tests") {
                    steps {
                        sh "dotnet cake ./build/build.cake --target=UnitTests --app-version=${APP_VERSION} --exclusive --verbosity=${params.CAKE_LOG_LEVEL}"
                    }
                }

                stage("Integration Tests") {
                    steps {
                        sh "dotnet cake ./build/build.cake --target=IntegrationTests --app-version=${APP_VERSION} --exclusive --verbosity=${params.CAKE_LOG_LEVEL}"
                    }
                }
            }

            post {
                always {
                    junit '.artifacts/test-results/**/*.junit'
                }
            }
        }

        stage("Code Coverage") {
            steps {
                sh "dotnet cake ./build/build.cake --target=CodeCoverage --app-version=${APP_VERSION} --exclusive --verbosity=${params.CAKE_LOG_LEVEL}"
                cobertura coberturaReportFile: '.artifacts/test-results/Cobertura.xml'
            }
        }

        stage("Package") {
            parallel {
                stage('Build Image') {
                    steps {
                        sh "dotnet cake ./build/build.cake --target=Publish --app-version=${APP_VERSION} --verbosity=${params.CAKE_LOG_LEVEL}  --exclusive"
                        sh "dotnet cake ./build/build.cake --target=BuildImage --app-version=${APP_VERSION} --verbosity=${params.CAKE_LOG_LEVEL} --commit-sha=${env.GIT_COMMIT} --build-url=${env.BUILD_URL} --exclusive"
                    }
                }
            }
        }

        stage("Docker Push to dev"){
            when{
                branch 'master'
            }
            environment{
                DEV_ROLE = ""
            }
            steps{
                container('trebuchet'){
                    // Using Trebuchet (https://github.com/HylandSoftware/trebuchet) to easily push images to ECR
                    sh "treb fling ${IMAGE_NAME}:${APP_VERSION} --as ${DEV_ROLE}"
                    script {
                        DEV_ECR_REPO = sh(script: "treb repo ${IMAGE_NAME} --as ${DEV_ROLE}", returnStdout: true).trim()
                    }
                    echo "${DEV_ECR_REPO}"
                }
            }
        }
        stage("Docker Push to prod"){
            when{
                branch 'master'
            }
            environment{
                PROD_ROLE = ""
            }
            steps{
                container('trebuchet'){
                    // Using Trebuchet (https://github.com/HylandSoftware/trebuchet) to easily push images to ECR
                    sh "treb fling ${IMAGE_NAME}:${APP_VERSION} --as ${PROD_ROLE}"
                    script {
                        PROD_ECR_REPO = sh(script: "treb repo ${IMAGE_NAME} --as ${PROD_ROLE}", returnStdout: true).trim()
                    }
                    echo "${PROD_ECR_REPO}"
                }
            }
        }


    }
}
