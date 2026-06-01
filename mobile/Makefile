# Makefile for Tannous POS Android App
# Provides shortcuts for common development tasks

.PHONY: help dev-build staging-aab prod-aab distribute-staging publish-internal clean test lint

# Default target
help:
	@echo "Tannous POS Android App - Available Commands"
	@echo "============================================="
	@echo ""
	@echo "Build Commands:"
	@echo "  dev-build          - Build development debug APK"
	@echo "  staging-aab        - Build staging release AAB"
	@echo "  prod-aab           - Build production release AAB"
	@echo ""
	@echo "Distribution Commands:"
	@echo "  distribute-staging - Upload staging AAB to Firebase App Distribution"
	@echo "  publish-internal   - Publish production AAB to Play Console internal track"
	@echo ""
	@echo "Development Commands:"
	@echo "  test               - Run unit tests"
	@echo "  lint               - Run lint checks"
	@echo "  clean              - Clean build artifacts"
	@echo "  version            - Show current version information"
	@echo ""
	@echo "CI/CD Commands:"
	@echo "  ci-setup           - Setup CI environment (create ci/ directory)"
	@echo "  changelog          - Generate release notes from Git commits"
	@echo ""

# Build Commands
dev-build:
	@echo "🔨 Building development debug APK..."
	./gradlew assembleDevDebug
	@echo "✅ Development build complete!"

staging-aab:
	@echo "🔨 Building staging release AAB..."
	./gradlew bundleStagingRelease
	@echo "✅ Staging AAB build complete!"

prod-aab:
	@echo "🔨 Building production release AAB..."
	./gradlew bundleProdRelease
	@echo "✅ Production AAB build complete!"

# Distribution Commands
distribute-staging:
	@echo "📱 Distributing staging AAB to Firebase App Distribution..."
	./gradlew appDistributionUploadStagingRelease
	@echo "✅ Staging distribution complete!"

publish-internal:
	@echo "🚀 Publishing production AAB to Play Console internal track..."
	./gradlew publishProdRelease -PplayTrack=internal
	@echo "✅ Play Console publishing complete!"

# Development Commands
test:
	@echo "🧪 Running unit tests..."
	./gradlew :core:test :app:test
	@echo "✅ Tests complete!"

lint:
	@echo "🔍 Running lint checks..."
	./gradlew :app:lint
	@echo "✅ Lint checks complete!"

clean:
	@echo "🧹 Cleaning build artifacts..."
	./gradlew clean
	@echo "✅ Clean complete!"

# Version and CI/CD Commands
version:
	@echo "📋 Version Information:"
	@./gradlew properties | grep -E "(versionName|versionCode)" || echo "Version not set"

ci-setup:
	@echo "🔧 Setting up CI environment..."
	@mkdir -p ci
	@echo "✅ CI directory created: ci/"

changelog:
	@echo "📝 Generating changelog..."
	@if [ "$(OS)" = "Windows_NT" ]; then \
		scripts\changelog.bat; \
	else \
		bash scripts/changelog.sh; \
	fi
	@echo "✅ Changelog generated!"

# Advanced Commands
build-all: dev-build staging-aab prod-aab
	@echo "🎉 All builds complete!"

distribute-all: distribute-staging publish-internal
	@echo "🎉 All distributions complete!"

# Development workflow
dev-workflow: clean test lint dev-build
	@echo "🎯 Development workflow complete!"

release-workflow: clean test lint staging-aab distribute-staging
	@echo "🎯 Release workflow complete!"

# Quick checks
check-env:
	@echo "🔍 Checking environment..."
	@echo "Java version:"
	@java -version 2>&1 | head -1
	@echo "Gradle version:"
	@./gradlew --version | grep "Gradle" | head -1
	@echo "Android SDK:"
	@echo "ANDROID_HOME: ${ANDROID_HOME:-Not set}"
	@echo "✅ Environment check complete!"

# Helpers
install-deps:
	@echo "📦 Installing dependencies..."
	./gradlew build --refresh-dependencies
	@echo "✅ Dependencies installed!"

# Show build variants
variants:
	@echo "📱 Available build variants:"
	@./gradlew tasks | grep -E "(assemble|bundle)" | grep -v "help" | sort

# Show signing info
signing:
	@echo "🔐 Signing information:"
	@./gradlew signingReport

# Show project properties
props:
	@echo "⚙️  Project properties:"
	@./gradlew properties | grep -E "(version|applicationId|buildType)" | sort
