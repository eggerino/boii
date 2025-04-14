cmake_minimum_required(VERSION 3.11)

project(boii)

add_library(libboii STATIC
    "src/cartridge.c"
    "src/bus.c"
)
target_include_directories(libboii PRIVATE "include")

add_executable(boii "src/main.c")
target_include_directories(boii PRIVATE "include")
target_link_libraries(boii libboii)
